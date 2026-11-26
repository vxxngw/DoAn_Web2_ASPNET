using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class EmployerWorkspaceTests(ApiFixture app)
{
    [Fact]
    public async Task Candidate_controls_directory_visibility_and_only_employers_can_search()
    {
        var candidate = await app.AccountAsync("Candidate");
        var employer = await app.AccountAsync("Employer");
        string directory = "/api/employer/candidates";
        string detail = $"{directory}/{candidate.Id}";
        string settings = "/api/auth/me/candidate-profile";
        var publish = new { isProfilePublic = true, professionalTitle = "Backend", profileSummary = "ASP.NET Core" };
        await ErrorAsync(await employer.Client.GetAsync(detail), HttpStatusCode.NotFound);
        await ErrorAsync(await employer.Client.PutAsJsonAsync(settings,publish), HttpStatusCode.Forbidden);
        await ErrorAsync(await candidate.Client.GetAsync(directory), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.GetAsync(directory), HttpStatusCode.Forbidden);
        var updated = await ReadAsync<UserResponse>(await candidate.Client.PutAsJsonAsync(settings,publish));
        Assert.True(updated.IsProfilePublic);
        var profile = await ReadAsync<PublicCandidate>(await employer.Client.GetAsync(detail));
        Assert.Equal("Backend", profile.ProfessionalTitle);
        var found = await ReadAsync<PagedResult<PublicCandidate>>(await employer.Client.GetAsync(directory+"?keyword="+candidate.Email));
        Assert.Equal(candidate.Id, Assert.Single(found.Items).Id);
        await ReadAsync<UserResponse>(await candidate.Client.PutAsJsonAsync(settings, new { isProfilePublic = false }));
        await ErrorAsync(await employer.Client.GetAsync(detail), HttpStatusCode.NotFound);
        var hidden = await ReadAsync<PagedResult<PublicCandidate>>(await employer.Client.GetAsync(directory+"?keyword="+candidate.Email));
        Assert.Empty(hidden.Items);
    }

    private static MultipartFormDataContent JobForm(byte[] pdf, string? version = null)
    {
        var form = new MultipartFormDataContent {
            {new StringContent("Tin có JD"), "title"}, {new StringContent("Thông tin vị trí"), "description"},
            {new StringContent("Hà Nội"), "location"}, {new StringContent(DateTimeOffset.UtcNow.AddDays(2).ToString("O")), "deadline"},
            {new ByteArrayContent(pdf), "Pdf", "jd.pdf"}
        };
        if (version != null) form.Add(new StringContent(version),"rowVersion");
        return form;
    }

    [Fact]
    public async Task Report_counts_all_jobs_even_when_the_requested_page_has_one_row()
    {
        var employer = await app.AccountAsync("Employer");
        await CreateJobAsync(employer,NewJob("Đang tuyển"));
        var closed = NewJob("Đã đóng");
        closed.IsOpen = false;
        await CreateJobAsync(employer,closed);
        var expired = await CreateJobAsync(employer,NewJob("Hết hạn"));
        await using var db = app.OpenDb();
        await db.Jobs.Where(j => j.Id == expired.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(j => j.Deadline,DateTimeOffset.UtcNow.AddDays(-1)));
        var report = await ReadAsync<EmployerReport>(await employer.Client.GetAsync("/api/employer/report?page=2&pageSize=1"));
        Assert.Equal(new EmployerJobTotals(1,1,1,0),report.Totals);
        Assert.Equal(3,report.Jobs.TotalCount);
        Assert.Single(report.Jobs.Items);
        Assert.Equal(0,report.TotalApplications);
    }

    [Fact]
    public async Task Job_attachment_can_be_created_preserved_replaced_and_removed()
    {
        var employer = await app.AccountAsync("Employer");
        var outsider = await app.AccountAsync("Employer");
        var draft = NewJob();
        draft.JdUrl = "file:///jd.pdf";
        await ErrorAsync(await employer.Client.PostAsJsonAsync("/api/jobs",draft),HttpStatusCode.BadRequest);
        using var invalid = JobForm("plain text"u8.ToArray());
        await ErrorAsync(await employer.Client.PostAsync("/api/jobs/with-jd",invalid),HttpStatusCode.BadRequest);
        using var oversized = JobForm(new byte[5 * 1024 * 1024 + 1]);
        await ErrorAsync(await employer.Client.PostAsync("/api/jobs/with-jd",oversized),HttpStatusCode.RequestEntityTooLarge);
        var before = await ReadAsync<EmployerReport>(await employer.Client.GetAsync("/api/employer/report"));
        Assert.Empty(before.Jobs.Items);
        using var upload = JobForm(ApplicationTests.Pdf);
        var created = await ReadAsync<CreatedResponse>(await employer.Client.PostAsync("/api/jobs/with-jd",upload),HttpStatusCode.Created);
        string path = "/api/jobs/"+created.Id;
        using var download = await app.Public.GetAsync(path+"/jd");
        Assert.Equal(HttpStatusCode.OK,download.StatusCode);
        Assert.Equal("inline", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ApplicationTests.Pdf,await download.Content.ReadAsByteArrayAsync());
        draft.JdUrl = null;
        Assert.Equal(HttpStatusCode.NoContent,(await employer.Client.PutAsJsonAsync(path,draft)).StatusCode);
        var preserved = await ReadAsync<JobResponse>(await app.Public.GetAsync(path));
        Assert.True(preserved.HasJdPdf);
        draft.JdUrl = "https://example.test/job";
        Assert.Equal(HttpStatusCode.NoContent,(await employer.Client.PutAsJsonAsync(path,draft)).StatusCode);
        var linked = await ReadAsync<JobResponse>(await app.Public.GetAsync(path));
        Assert.Equal(draft.JdUrl,linked.JdUrl);
        Assert.False(linked.HasJdPdf);
        await ErrorAsync(await app.Public.GetAsync(path+"/jd"),HttpStatusCode.NotFound);
        using var replace = JobForm(ApplicationTests.Pdf,Convert.ToBase64String(linked.RowVersion));
        await ErrorAsync(await outsider.Client.PutAsync(path+"/with-jd",replace),HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.NoContent,(await employer.Client.PutAsync(path+"/with-jd",replace)).StatusCode);
        var pdf = await ReadAsync<JobResponse>(await app.Public.GetAsync(path));
        Assert.True(pdf.HasJdPdf);
        Assert.Null(pdf.JdUrl);
        var clear = new { draft.Title, draft.Location, draft.Description, draft.Deadline, removeJd = true };
        Assert.Equal(HttpStatusCode.NoContent,(await employer.Client.PutAsJsonAsync(path,clear)).StatusCode);
        await ErrorAsync(await app.Public.GetAsync(path+"/jd"),HttpStatusCode.NotFound);
    }
}
