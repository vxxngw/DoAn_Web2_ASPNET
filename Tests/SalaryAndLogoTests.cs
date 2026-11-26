using System.Net;
using System.Net.Http.Json;
using Recruitment.Api.DTOs;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class SalaryAndLogoTests(ApiFixture app)
{
    [Fact]
    public async Task Salary_survives_legacy_edits_and_is_consistent_on_cards_and_saved_jobs()
    {
        var owner = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var draft = NewJob();
        var old = await CreateJobAsync(owner,draft);
        Assert.Null(old.IsSalaryNegotiable);
        var route = $"/api/jobs/{old.Id}";
        foreach (var (mode, low, high) in new (bool?, int?, int?)[] {
            (false,null,10), (false,10,null), (false,20,10), (false,0,10), (true,10,20), (null,10,20) })
        {
            draft.IsSalaryNegotiable = mode;
            draft.SalaryMin = low;
            draft.SalaryMax = high;
            await ErrorAsync(await owner.Client.PutAsJsonAsync(route,draft),HttpStatusCode.BadRequest);
        }
        draft.IsSalaryNegotiable = false;
        draft.SalaryMin = 12_000_000;
        draft.SalaryMax = 18_000_000;
        Assert.Equal(HttpStatusCode.NoContent,(await owner.Client.PutAsJsonAsync(route,draft)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await app.Admin.Client.PutAsJsonAsync(route,NewJob())).StatusCode);
        var row = Assert.Single((await ReadAsync<PagedResult<JobResponse>>(await app.Public.GetAsync($"/api/jobs?employerId={owner.Id}"))).Items);
        Assert.Equal((false,12_000_000,18_000_000),(row.IsSalaryNegotiable,row.SalaryMin,row.SalaryMax));
        Assert.Equal(HttpStatusCode.NoContent,(await candidate.Client.PutAsync($"/api/saved-jobs/{old.Id}",null)).StatusCode);
        var saved = Assert.Single((await ReadAsync<PagedResult<SavedJobItem>>(await candidate.Client.GetAsync("/api/saved-jobs"))).Items);
        Assert.Equal((row.IsSalaryNegotiable,row.SalaryMin,row.SalaryMax),(saved.IsSalaryNegotiable,saved.SalaryMin,saved.SalaryMax));
    }

    [Fact]
    public async Task Company_logo_is_scoped_to_its_employer_and_independent_of_the_personal_avatar()
    {
        var owner = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var other = await app.AccountAsync("Employer");
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aScsAAAAASUVORK5CYII=");
        static MultipartFormDataContent Image(byte[] bytes) => new() { { new ByteArrayContent(bytes), "image", "logo.png" } };
        const string upload = "/api/companies/me/logo";
        string read = $"/api/companies/{owner.Id}/logo";
        await ErrorAsync(await app.Public.GetAsync(read),HttpStatusCode.NotFound);
        using var good = Image(png);
        await ErrorAsync(await candidate.Client.PutAsync(upload,good),HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.PutAsync(upload,good),HttpStatusCode.Forbidden);
        using var invalid = Image("invalid"u8.ToArray());
        await ErrorAsync(await owner.Client.PutAsync(upload,invalid),HttpStatusCode.BadRequest);
        using var large = Image(new byte[1024*1024+1]);
        await ErrorAsync(await owner.Client.PutAsync(upload,large),HttpStatusCode.RequestEntityTooLarge);
        Assert.Equal(HttpStatusCode.NoContent,(await owner.Client.PutAsync(upload,good)).StatusCode);
        Assert.Null((await ReadAsync<UserResponse>(await owner.Client.GetAsync("/api/auth/me"))).AvatarDataUrl);
        Assert.False((await ReadAsync<UserResponse>(await other.Client.GetAsync("/api/auth/me"))).HasCompanyLogo);
        var top = await ReadAsync<List<TopCompany>>(await app.Public.GetAsync("/api/jobs/top-companies"));
        int count = (top.FirstOrDefault()?.OpenJobs ?? 0) + 1;
        for (int i = 0; i < count; i++) await CreateJobAsync(owner,NewJob());
        var ranked = (await ReadAsync<List<TopCompany>>(await app.Public.GetAsync("/api/jobs/top-companies")))[0];
        Assert.Equal(owner.Id,ranked.EmployerId);
        Assert.True(ranked.HasCompanyLogo);
        var jobs = await ReadAsync<PagedResult<JobResponse>>(await app.Public.GetAsync($"/api/jobs?employerId={owner.Id}"));
        Assert.All(jobs.Items, job => Assert.True(job.HasCompanyLogo));
        var jobId = jobs.Items[0].Id;
        Assert.True((await ReadAsync<JobResponse>(await app.Public.GetAsync($"/api/jobs/{jobId}"))).HasCompanyLogo);
        Assert.Equal(HttpStatusCode.NoContent,(await candidate.Client.PutAsync($"/api/saved-jobs/{jobId}",null)).StatusCode);
        var saved = Assert.Single((await ReadAsync<PagedResult<SavedJobItem>>(await candidate.Client.GetAsync("/api/saved-jobs"))).Items);
        Assert.Equal(owner.Id,saved.EmployerId);
        Assert.True(saved.HasCompanyLogo);
        Assert.Equal(png,await app.Public.GetByteArrayAsync(read));
        Assert.Equal(HttpStatusCode.NoContent,(await app.Admin.Client.PatchAsJsonAsync($"/api/admin/users/{owner.Id}/lock",new {isLocked=true})).StatusCode);
        Assert.False(Assert.Single((await ReadAsync<PagedResult<SavedJobItem>>(await candidate.Client.GetAsync("/api/saved-jobs"))).Items).HasCompanyLogo);
        await ErrorAsync(await app.Public.GetAsync(read),HttpStatusCode.NotFound);
    }
}
