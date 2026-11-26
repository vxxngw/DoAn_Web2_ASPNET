using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class ApplicationTests(ApiFixture app)
{
    internal static byte[] Pdf => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "cv-sample.pdf"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_five_MiB_pdf_can_be_uploaded_and_downloaded_intact(bool chunked)
    {
        // Thêm khoảng trắng trước startxref để PDF vẫn hợp lệ và đạt đúng 5 MiB.
        var sample = Pdf;
        int tail = Encoding.ASCII.GetString(sample).LastIndexOf("startxref", StringComparison.Ordinal);
        var file = new byte[5 * 1024 * 1024];
        Array.Fill(file, (byte)' ');
        sample.AsSpan(0, tail).CopyTo(file);
        sample.AsSpan(tail).CopyTo(file.AsSpan(file.Length - (sample.Length - tail)));

        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(await app.AccountAsync("Employer"), NewJob());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/jobs/{job.Id}/applications") { Content = Form(file) };
        if (chunked) request.Headers.TransferEncodingChunked = true;
        var created = await ReadAsync<CreatedResponse>(await candidate.Client.SendAsync(request), HttpStatusCode.Created);
        var downloaded = await candidate.Client.GetByteArrayAsync($"/api/applications/{created.Id}/cv");
        Assert.True(file.AsSpan().SequenceEqual(downloaded));
    }

    [Fact]
    public async Task Apply_requires_a_multipart_form_and_an_existing_job()
    {
        var candidate = await app.AccountAsync("Candidate");
        const string missing = "/api/jobs/2147483647/applications";
        await ErrorAsync(await candidate.Client.PostAsJsonAsync(missing, new {message="CV"}), HttpStatusCode.UnsupportedMediaType);
        using var form = Form();
        await ErrorAsync(await candidate.Client.PostAsync(missing, form), HttpStatusCode.NotFound);
    }

    internal static MultipartFormDataContent Form(byte[]? bytes = null, string fileName = "cv.pdf", string message = "CV của tôi")
    {
        var file = new ByteArrayContent(bytes ?? Pdf);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        return new MultipartFormDataContent { {file, "cv", fileName}, {new StringContent(message), "message"} };
    }

    [Theory]
    [InlineData("extension", HttpStatusCode.BadRequest)]
    [InlineData("signature", HttpStatusCode.BadRequest)]
    [InlineData("missing-message", HttpStatusCode.BadRequest)]
    [InlineData("over-file-limit", HttpStatusCode.RequestEntityTooLarge)]
    [InlineData("over-request-limit", HttpStatusCode.RequestEntityTooLarge)]
    [InlineData("chunked-over-limit", HttpStatusCode.RequestEntityTooLarge)]
    public async Task Invalid_uploads_return_a_clear_error_and_do_not_create_a_record(string kind, HttpStatusCode expected)
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());
        byte[] bytes = kind switch
        {
            "signature" => Encoding.ASCII.GetBytes("ordinary text"),
            "over-file-limit" => new byte[5 * 1024 * 1024 + 1],
            "over-request-limit" or "chunked-over-limit" => new byte[6 * 1024 * 1024 + 1],
            _ => Pdf
        };
        using var form = Form(bytes, kind == "extension" ? "cv.txt" : "cv.pdf", kind == "missing-message" ? "" : "CV");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/jobs/{job.Id}/applications") { Content = form };
        if (kind == "chunked-over-limit")
            request.Headers.TransferEncodingChunked = true;
        await ErrorAsync(await candidate.Client.SendAsync(request), expected);
        var mine = await ReadAsync<PagedResult<ApplicationResponse>>(await candidate.Client.GetAsync("/api/applications/mine"));
        Assert.Equal(0, mine.TotalCount);
    }

    [Fact]
    public async Task Only_the_applicant_and_job_owner_can_read_the_application_and_download_its_pdf()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var foreignEmployer = await app.AccountAsync("Employer");
        var foreignCandidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());
        string list = $"/api/jobs/{job.Id}/applications";
        using var form = Form();
        using var created = await candidate.Client.PostAsync(list, form);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        string detail = created.Headers.Location!.AbsolutePath;
        var application = await ReadAsync<ApplicationResponse>(await candidate.Client.GetAsync(detail));
        Assert.Equal(candidate.Email, application.CandidateEmail);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        foreach (var actor in new[] {candidate, employer})
        {
            using var pdf = await actor.Client.GetAsync(detail + "/cv");
            Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.Equal("attachment", pdf.Content.Headers.ContentDisposition?.DispositionType);
            Assert.True(pdf.Headers.CacheControl?.NoStore);
            Assert.Equal(Pdf, await pdf.Content.ReadAsByteArrayAsync());
        }
        await ReadAsync<UserResponse>(await candidate.Client.PutAsJsonAsync("/api/auth/me/candidate-profile", new {isProfilePublic = true}));
        var publicProfile = await ReadAsync<PublicCandidate>(await foreignEmployer.Client.GetAsync($"/api/employer/candidates/{candidate.Id}"));
        Assert.Equal(candidate.Email, publicProfile.Email);
        foreach (var actor in new[] {foreignEmployer, foreignCandidate, app.Admin})
        {
            await ErrorAsync(await actor.Client.GetAsync(detail), HttpStatusCode.NotFound);
            await ErrorAsync(await actor.Client.GetAsync(detail + "/cv"), HttpStatusCode.NotFound);
        }
        await ErrorAsync(await app.Public.GetAsync(detail + "/cv"), HttpStatusCode.Unauthorized);
        await ErrorAsync(await foreignEmployer.Client.GetAsync(list), HttpStatusCode.Forbidden);
        await ErrorAsync(await candidate.Client.PatchAsJsonAsync(detail + "/status", new {status="Accepted"}), HttpStatusCode.Forbidden);
        await ErrorAsync(await foreignEmployer.Client.PatchAsJsonAsync(detail + "/status", new {status="Accepted"}), HttpStatusCode.NotFound);
        await ErrorAsync(await employer.Client.PatchAsJsonAsync(detail + "/status", new {status="Submitted"}), HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.NoContent, (await employer.Client.PatchAsJsonAsync(detail + "/status", new {status="Rejected"})).StatusCode);
        var reviewed = await ReadAsync<PagedResult<ApplicationResponse>>(await candidate.Client.GetAsync($"/api/applications/mine?status=Rejected&jobId={job.Id}"));
        Assert.Equal(application.Id, Assert.Single(reviewed.Items).Id);
        var waiting = await ReadAsync<PagedResult<ApplicationResponse>>(await employer.Client.GetAsync(list+"?status=Submitted"));
        Assert.Equal(0, waiting.TotalCount);
        Assert.Equal(HttpStatusCode.NoContent, (await employer.Client.DeleteAsync($"/api/jobs/{job.Id}")).StatusCode);
        await ErrorAsync(await app.Public.GetAsync($"/api/jobs/{job.Id}"), HttpStatusCode.NotFound);
        Assert.True((await ReadAsync<JobResponse>(await employer.Client.GetAsync($"/api/jobs/{job.Id}"))).Archived);
        Assert.Equal(Pdf, await employer.Client.GetByteArrayAsync(detail + "/cv"));
        var report = await ReadAsync<EmployerReport>(await employer.Client.GetAsync("/api/employer/report"));
        Assert.Equal(new EmployerJobTotals(0,0,0,1), report.Totals);
        Assert.Equal(1, report.TotalApplications);
        Assert.Equal(1, Assert.Single(report.Jobs.Items).Applications);
        var own = await ReadAsync<PagedResult<ApplicationResponse>>(await employer.Client.GetAsync("/api/employer/applications?keyword="+candidate.Email));
        Assert.Equal(application.Id, Assert.Single(own.Items).Id);
        var other = await ReadAsync<EmployerReport>(await foreignEmployer.Client.GetAsync("/api/employer/report"));
        Assert.Equal(0, other.TotalApplications);
        Assert.Empty((await ReadAsync<PagedResult<ApplicationResponse>>(await foreignEmployer.Client.GetAsync("/api/employer/applications"))).Items);
        var impact = await ReadAsync<DeletionImpact>(await app.Admin.Client.GetAsync($"/api/admin/jobs/{job.Id}/deletion-impact"));
        Assert.Equal(new DeletionImpact(1, 1), impact);
        await using (var db = app.OpenDb())
            Assert.NotNull((await db.Applications.FindAsync(application.Id))!.ReviewedAt);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.DeleteAsync($"/api/jobs/{job.Id}")).StatusCode);
        await ErrorAsync(await candidate.Client.GetAsync(detail + "/cv"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Concurrent_submissions_and_reviews_each_commit_once()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());
        string list = $"/api/jobs/{job.Id}/applications";
        using var first = Form();
        using var second = Form();
        var posts = await Task.WhenAll(candidate.Client.PostAsync(list, first), candidate.Client.PostAsync(list, second));
        Assert.Equal(new[] {HttpStatusCode.Created, HttpStatusCode.Conflict}, posts.Select(p => p.StatusCode).Order().ToArray());
        var pending = await ReadAsync<PagedResult<ApplicationResponse>>(await employer.Client.GetAsync(list));
        var detail = $"/api/applications/{Assert.Single(pending.Items).Id}";
        var choices = new[] {"Accepted", "Rejected"};
        var decisions = await Task.WhenAll(choices.Select(status => employer.Client.PatchAsJsonAsync(detail+"/status", new {status})));
        int winner = Array.FindIndex(decisions, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.InRange(winner, 0, 1);
        Assert.Equal(HttpStatusCode.Conflict, decisions[1 - winner].StatusCode);
        var final = await ReadAsync<ApplicationResponse>(await candidate.Client.GetAsync(detail));
        Assert.Equal(choices[winner], final.Status.ToString());
    }
}
