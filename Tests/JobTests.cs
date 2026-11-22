using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class JobTests(ApiFixture app)
{
    [Fact]
    public async Task Search_sort_and_pagination_return_matching_rows_and_total()
    {
        var employer = await app.AccountAsync("Employer");
        string prefix = Guid.NewGuid().ToString("N");
        foreach (var (title, city) in new[] {("C","Huế"), ("A","Huế"), ("B","Đà Nẵng")})
            await CreateJobAsync(employer, NewJob(prefix + title, city));
        var result = await ReadAsync<PagedResult<JobResponse>>(await app.Public.GetAsync(
            $"/api/jobs?keyword={prefix}&location=Hu%E1%BA%BF&sort=Title&pageSize=1&page=2"));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(prefix + "C", Assert.Single(result.Items).Title);
        var mine = await ReadAsync<PagedResult<JobResponse>>(await employer.Client.GetAsync("/api/jobs/mine"));
        Assert.Equal(3, mine.TotalCount);
        await ErrorAsync(await app.Public.GetAsync("/api/jobs?pageSize=101"), HttpStatusCode.BadRequest);
        await ErrorAsync(await app.Public.GetAsync("/api/jobs?sort=Unknown"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Closing_and_expiry_remove_jobs_from_public_results_and_summary()
    {
        var employer = await app.AccountAsync("Employer");
        var summary = await ReadAsync<JobSummary>(await app.Public.GetAsync("/api/jobs/summary"));
        var draft = NewJob();
        var job = await CreateJobAsync(employer, draft);
        var open = await ReadAsync<JobSummary>(await app.Public.GetAsync("/api/jobs/summary"));
        Assert.Equal(summary.OpenJobs + 1, open.OpenJobs);
        Assert.Equal(summary.Companies + 1, open.Companies);
        draft.IsOpen = false;
        var route = $"/api/jobs/{job.Id}";
        Assert.Equal(HttpStatusCode.NoContent, (await employer.Client.PutAsJsonAsync(route,draft)).StatusCode);
        await ErrorAsync(await app.Public.GetAsync(route), HttpStatusCode.NotFound);
        Assert.False((await ReadAsync<JobResponse>(await employer.Client.GetAsync(route))).IsOpen);
        var moderated = await ReadAsync<PagedResult<JobResponse>>(await app.Admin.Client.GetAsync("/api/admin/jobs?sort=Newest&pageSize=1"));
        Assert.Equal(job.Id, Assert.Single(moderated.Items).Id);
        await using var db = app.OpenDb();
        // Đặt hạn vào hôm qua để kiểm tra hết hạn mà không phải chờ đồng hồ.
        await db.Jobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(set => set
            .SetProperty(j => j.Deadline, DateTimeOffset.UtcNow.AddDays(-1)).SetProperty(j => j.IsOpen, true));
        await ErrorAsync(await app.Public.GetAsync(route), HttpStatusCode.NotFound);
        var expired = await ReadAsync<JobSummary>(await app.Public.GetAsync("/api/jobs/summary"));
        Assert.Equal(summary.OpenJobs, expired.OpenJobs);
        var candidate = await app.AccountAsync("Candidate");
        using var form = ApplicationTests.Form();
        await ErrorAsync(await candidate.Client.PostAsync(route+"/applications",form), HttpStatusCode.Conflict);
        draft.IsOpen = true;
        draft.Deadline = DateTimeOffset.UtcNow.AddDays(-1);
        await ErrorAsync(await employer.Client.PostAsJsonAsync("/api/jobs", draft), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Owner_and_admin_can_edit_but_stale_versions_and_foreign_employers_are_rejected()
    {
        var employer = await app.AccountAsync("Employer");
        var outsider = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());
        var route = $"/api/jobs/{job.Id}";
        var update = new UpdateJobRequest {Title="Đã sửa", Description=job.Description, Location=job.Location,
            Deadline=job.Deadline, RowVersion=job.RowVersion};
        await ErrorAsync(await outsider.Client.PutAsJsonAsync(route, update), HttpStatusCode.Forbidden);
        await ErrorAsync(await candidate.Client.PostAsJsonAsync("/api/jobs", update), HttpStatusCode.Forbidden);
        await ErrorAsync(await outsider.Client.DeleteAsync(route), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.PutAsJsonAsync(route,update)).StatusCode);
        update.Title = "Phiên bản cũ";
        await ErrorAsync(await employer.Client.PutAsJsonAsync(route,update), HttpStatusCode.Conflict);
        Assert.Equal("Đã sửa", (await ReadAsync<JobResponse>(await app.Public.GetAsync(route))).Title);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.DeleteAsync(route)).StatusCode);
        await ErrorAsync(await app.Public.GetAsync(route), HttpStatusCode.NotFound);
    }
}
