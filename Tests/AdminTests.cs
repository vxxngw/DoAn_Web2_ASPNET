using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class AdminTests(ApiFixture app)
{
    [Fact]
    public async Task Account_tables_filter_by_role_before_counting_and_paging()
    {
        var tag = Guid.NewGuid().ToString("N");
        var api = app.Admin.Client;
        foreach (var (suffix, role) in new[] { ("a","Candidate"), ("b","Candidate"), ("c","Employer") })
        {
            var input = new { email = $"{tag}{suffix}@example.test", password = Password, fullName = suffix,
                role, companyName = role == "Employer" ? $"Company-{tag}" : null };
            using var created = await app.Public.PostAsJsonAsync("/api/auth/register", input);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }
        string path = $"/api/admin/users?keyword={tag}&pageSize=1";
        var second = await ReadAsync<PagedResult<AccountSummary>>(await api.GetAsync(path + "&role=Candidate&page=2"));
        Assert.Equal(2, second.TotalCount);
        Assert.Equal("b", Assert.Single(second.Items).FullName);
        var companies = await ReadAsync<PagedResult<AccountSummary>>(await api.GetAsync(path + "&role=Employer"));
        Assert.Equal(1, companies.TotalCount);
        Assert.Equal(UserRole.Employer, Assert.Single(companies.Items).Role);
        var byCompany = await ReadAsync<PagedResult<AccountSummary>>(await api.GetAsync($"/api/admin/users?role=Employer&keyword=Company-{tag}"));
        Assert.Equal(companies.Items[0].Id, Assert.Single(byCompany.Items).Id);
        var all = await ReadAsync<PagedResult<AccountSummary>>(await api.GetAsync(path));
        Assert.Equal(3, all.TotalCount);
        await ErrorAsync(await api.GetAsync(path + "&role=Unknown"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_edits_profiles_with_unique_email_and_protected_roles()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());
        string path = $"/api/admin/users/{employer.Id}";
        string email = $"{Guid.NewGuid():N}@example.test";
        var edit = new { fullName = "Tên mới", email, companyName = "Công ty mới" };
        await ErrorAsync(await employer.Client.PutAsJsonAsync(path, edit), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.PutAsJsonAsync(path, new { fullName = "A", email }), HttpStatusCode.BadRequest);
        await ErrorAsync(await app.Admin.Client.PutAsJsonAsync(path, new { fullName = "A", candidate.Email, companyName = "CT" }), HttpStatusCode.Conflict);
        await ErrorAsync(await app.Admin.Client.PutAsJsonAsync($"/api/admin/users/{app.Admin.Id}", edit), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.PutAsJsonAsync($"/api/admin/users/{candidate.Id}", edit), HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.PutAsJsonAsync(path, edit)).StatusCode);
        Assert.Equal(employer.Id, (await app.LoginAsync(email)).Id);
        Assert.Equal(edit.companyName, (await ReadAsync<JobResponse>(await app.Public.GetAsync($"/api/jobs/{job.Id}"))).CompanyName);
        Assert.Equal(edit.fullName, (await ReadAsync<AccountSummary>(await app.Admin.Client.GetAsync(path))).FullName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Admin_deletes_the_selected_account_and_exactly_its_related_rows(bool removeEmployer)
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var other = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());
        using var cv = ApplicationTests.Form();
        using var otherCv = ApplicationTests.Form();
        var one = await ReadAsync<CreatedResponse>(await candidate.Client.PostAsync($"/api/jobs/{job.Id}/applications",cv),HttpStatusCode.Created);
        var two = await ReadAsync<CreatedResponse>(await other.Client.PostAsync($"/api/jobs/{job.Id}/applications",otherCv),HttpStatusCode.Created);
        var target = removeEmployer ? employer : candidate;
        string path = $"/api/admin/users/{target.Id}";
        var impact = await ReadAsync<DeletionImpact>(await app.Admin.Client.GetAsync(path + "/deletion-impact"));
        Assert.Equal(removeEmployer ? new DeletionImpact(1,2) : new DeletionImpact(0,1), impact);
        await ErrorAsync(await target.Client.DeleteAsync(path), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.DeleteAsync($"/api/admin/users/{app.Admin.Id}"), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.DeleteAsync(path)).StatusCode);
        await ErrorAsync(await target.Client.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);
        await ErrorAsync(await app.Admin.Client.DeleteAsync(path), HttpStatusCode.NotFound);
        await using var db = app.OpenDb();
        Assert.False(await db.Users.AnyAsync(u => u.Id == target.Id));
        Assert.False(await db.Applications.AnyAsync(a => a.Id == one.Id));
        Assert.Equal(!removeEmployer, await db.Jobs.AnyAsync(j => j.Id == job.Id));
        Assert.Equal(!removeEmployer, await db.Applications.AnyAsync(a => a.Id == two.Id));
        Assert.True(await db.Users.AnyAsync(u => u.Id == other.Id));
    }

    [Fact]
    public async Task Reports_use_Vietnam_calendar_boundaries_and_actual_review_dates()
    {
        var client = app.Admin.Client;
        const string path = "/api/admin/statistics";
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        await ErrorAsync(await employer.Client.GetAsync(path), HttpStatusCode.Forbidden);
        await ErrorAsync(await client.GetAsync(path + "?period=Year"), HttpStatusCode.BadRequest);
        var before = await ReadAsync<AdminStatistics>(await client.GetAsync(path + "?period=All"));
        await using var db = app.OpenDb();
        var midnight = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.FromHours(7));
        var job = new Job { EmployerId = employer.Id, Title = "Báo cáo", CreatedAt = midnight.AddDays(-2), Deadline = midnight.AddDays(10) };
        var older = new Job { EmployerId = employer.Id, Title = "Lịch sử", CreatedAt = midnight.AddDays(-20), Deadline = midnight.AddDays(10) };
        db.Applications.AddRange(
            new JobApplication { Job = job, CandidateId = candidate.Id, SubmittedAt = midnight.AddTicks(-1),
                Status = ApplicationStatus.Accepted, ReviewedAt = midnight },
            new JobApplication { Job = older, CandidateId = candidate.Id, SubmittedAt = midnight.AddDays(-8),
                Status = ApplicationStatus.Rejected, ReviewedAt = null });
        await db.SaveChangesAsync();
        var day = await ReadAsync<AdminStatistics>(await client.GetAsync(path + "?period=Day&date=2024-02-29"));
        Assert.Equal((0,1,0,0), (day.JobsPosted,day.ApplicationsSubmitted,day.Accepted,day.Rejected));
        var march = await ReadAsync<AdminStatistics>(await client.GetAsync(path + "?period=Month&date=2024-03-10"));
        Assert.Equal((0,0,1,0), (march.JobsPosted,march.ApplicationsSubmitted,march.Accepted,march.Rejected));
        Assert.Equal(new DateOnly(2024,3,31), march.EndDateInclusive);
        var week = await ReadAsync<AdminStatistics>(await client.GetAsync(path + "?period=Week&date=2024-03-03"));
        Assert.Equal((1,1,1,0), (week.JobsPosted,week.ApplicationsSubmitted,week.Accepted,week.Rejected));
        Assert.Equal(new DateOnly(2024,2,26), week.StartDate);
        Assert.Equal(new DateOnly(2024,3,3), week.EndDateInclusive);
        var all = await ReadAsync<AdminStatistics>(await client.GetAsync(path + "?period=All"));
        Assert.Equal(before.Accepted + 1, all.Accepted);
        Assert.Equal(before.Rejected + 1, all.Rejected);
        Assert.Equal(before.ReviewsWithoutDate + 1, all.ReviewsWithoutDate);
    }
}
