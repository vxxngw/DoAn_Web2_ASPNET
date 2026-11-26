using System.Net;
using System.Net.Http.Json;
using Recruitment.Api.DTOs;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class SavedJobTests(ApiFixture app)
{
    [Fact]
    public async Task Saves_are_private_repeatable_and_closed_jobs_can_still_be_removed()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var other = await app.AccountAsync("Candidate");
        var draft = NewJob();
        var job = await CreateJobAsync(employer,draft);
        string saved = $"/api/saved-jobs/{job.Id}";
        string detail = $"/api/jobs/{job.Id}";
        foreach (var client in new[] { employer.Client, app.Admin.Client })
        {
            await ErrorAsync(await client.PutAsync(saved,null),HttpStatusCode.Forbidden);
            await ErrorAsync(await client.GetAsync("/api/saved-jobs"),HttpStatusCode.Forbidden);
            await ErrorAsync(await client.DeleteAsync(saved),HttpStatusCode.Forbidden);
        }
        var responses = await Task.WhenAll(candidate.Client.PutAsync(saved,null),candidate.Client.PutAsync(saved,null));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NoContent,r.StatusCode));
        Assert.True((await ReadAsync<JobResponse>(await candidate.Client.GetAsync(detail))).IsSaved);
        Assert.False((await ReadAsync<JobResponse>(await other.Client.GetAsync(detail))).IsSaved);
        var list = await ReadAsync<PagedResult<JobResponse>>(await candidate.Client.GetAsync($"/api/jobs?employerId={employer.Id}"));
        Assert.True(Assert.Single(list.Items).IsSaved);
        Assert.Equal(HttpStatusCode.NoContent,(await other.Client.DeleteAsync(saved)).StatusCode);
        Assert.Empty((await ReadAsync<PagedResult<SavedJobItem>>(await other.Client.GetAsync("/api/saved-jobs"))).Items);
        draft.IsOpen = false;
        Assert.Equal(HttpStatusCode.NoContent,(await employer.Client.PutAsJsonAsync(detail,draft)).StatusCode);
        Assert.False(Assert.Single((await ReadAsync<PagedResult<SavedJobItem>>(await candidate.Client.GetAsync("/api/saved-jobs"))).Items).IsOpen);
        await ErrorAsync(await other.Client.PutAsync(saved,null),HttpStatusCode.NotFound);
        for (int i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.NoContent,(await candidate.Client.DeleteAsync(saved)).StatusCode);
        Assert.Empty((await ReadAsync<PagedResult<SavedJobItem>>(await candidate.Client.GetAsync("/api/saved-jobs"))).Items);
    }

    [Fact]
    public async Task Industry_counts_and_city_filters_only_include_matching_open_jobs()
    {
        var employer = await app.AccountAsync("Employer");
        var draft = NewJob();
        draft.Industry = "Ngành kiểm thử " + Guid.NewGuid().ToString("N");
        var first = await CreateJobAsync(employer,draft);
        draft.Location = "Huế";
        await CreateJobAsync(employer,draft);
        draft.IsOpen = false;
        await CreateJobAsync(employer,draft);
        var industries = await ReadAsync<List<IndustrySummary>>(await app.Public.GetAsync("/api/jobs/industries"));
        Assert.Equal(2,Assert.Single(industries, i => i.Industry == draft.Industry).OpenJobs);
        string query = $"/api/jobs?sort=Featured&industry={Uri.EscapeDataString(draft.Industry)}&location={Uri.EscapeDataString(first.Location)}";
        Assert.Equal(first.Id,Assert.Single((await ReadAsync<PagedResult<JobResponse>>(await app.Public.GetAsync(query))).Items).Id);
    }
}
