using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class AccountTests(ApiFixture app)
{
    [Fact]
    public async Task Migrations_preserve_existing_users_and_match_model()
    {
        await using var db = app.OpenDb();
        Assert.False((await db.Users.SingleAsync(u => u.Email == "legacy@example.test")).IsLocked);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        await db.Database.MigrateAsync();
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == "legacy@example.test"));
    }

    [Theory]
    [InlineData("bad-email", "Candidate", "Người dùng", "Enough123", null)]
    [InlineData("a@example.test", "Candidate", "  ", "Enough123", null)]
    [InlineData("a@example.test", "Candidate", "An", "short", null)]
    [InlineData("a@example.test", "Employer", "An", "Enough123", "  ")]
    [InlineData("a@example.test", "Candidate", "An", "Enough123", "Công ty")]
    [InlineData("a@example.test", "Admin", "An", "Enough123", null)]
    public async Task Invalid_registrations_return_400(string email, string role, string fullName, string password, string? companyName)
    {
        await ErrorAsync(await app.Public.PostAsJsonAsync("/api/auth/register", new {email, role, fullName, password, companyName}), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Registration_normalizes_email_hashes_password_and_login_returns_the_correct_identity()
    {
        var account = await app.AccountAsync("Candidate");
        var duplicate = Registration(account.Email.ToUpperInvariant(), "Candidate");
        await ErrorAsync(await app.Public.PostAsJsonAsync("/api/auth/register", duplicate), HttpStatusCode.Conflict);
        await ErrorAsync(await app.Public.PostAsJsonAsync("/api/auth/login", new {account.Email, password = "wrong"}), HttpStatusCode.Unauthorized);
        var login = await app.LoginAsync(account.Email.ToUpperInvariant());
        var me = await ReadAsync<JsonElement>(await login.Client.GetAsync("/api/auth/me"));
        Assert.Equal(account.Id, me.GetProperty("id").GetInt32());
        Assert.Equal(account.Email, me.GetProperty("email").GetString());
        Assert.False(me.TryGetProperty("passwordHash", out _));
        await using var db = app.OpenDb();
        var stored = await db.Users.FindAsync(account.Id);
        Assert.NotEqual(Password, stored!.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<User>().VerifyHashedPassword(stored, stored.PasswordHash, Password));
    }

    [Fact]
    public async Task Administrator_lock_controls_login_existing_session_and_job_visibility()
    {
        var employer = await app.AccountAsync("Employer");
        var job = await CreateJobAsync(employer, NewJob());
        var users = await ReadAsync<PagedResult<AccountSummary>>(await app.Admin.Client.GetAsync("/api/admin/users?keyword=" + Uri.EscapeDataString(employer.Email)));
        Assert.Equal(employer.Id, Assert.Single(users.Items).Id);
        var route = $"/api/admin/users/{employer.Id}/lock";
        await ErrorAsync(await employer.Client.PatchAsJsonAsync(route, new {isLocked = false}), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.PatchAsJsonAsync(route, new {}), HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.PatchAsJsonAsync(route, new {isLocked = true})).StatusCode);
        await ErrorAsync(await employer.Client.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);
        await ErrorAsync(await app.Public.PostAsJsonAsync("/api/auth/login", new { employer.Email, password = Password }), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Public.GetAsync($"/api/jobs/{job.Id}"), HttpStatusCode.NotFound);
        var all = await ReadAsync<JobResponse>(await app.Admin.Client.GetAsync($"/api/jobs/{job.Id}"));
        Assert.True(all.EmployerLocked);
        var candidate = await app.AccountAsync("Candidate");
        using var form = ApplicationTests.Form();
        await ErrorAsync(await candidate.Client.PostAsync($"/api/jobs/{job.Id}/applications", form), HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.PatchAsJsonAsync(route, new {isLocked = false})).StatusCode);
        Assert.Equal(employer.Id, (await app.LoginAsync(employer.Email)).Id);
        Assert.Equal(HttpStatusCode.OK, (await app.Public.GetAsync($"/api/jobs/{job.Id}")).StatusCode);
        await ErrorAsync(await app.Admin.Client.PatchAsJsonAsync($"/api/admin/users/{app.Admin.Id}/lock", new {isLocked = true}), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.PatchAsJsonAsync("/api/admin/users/2147483647/lock", new {isLocked = true}), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Administrator_can_delete_accounts_and_cascade_employer_history_but_not_candidate_history_or_admins()
    {
        var employer = await app.AccountAsync("Employer");
        var job = await CreateJobAsync(employer, NewJob());
        var candidate = await app.AccountAsync("Candidate");
        using var form = ApplicationTests.Form();
        using var created = await candidate.Client.PostAsync($"/api/jobs/{job.Id}/applications", form);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var otherEmployer = await app.AccountAsync("Employer");
        var otherJob = await CreateJobAsync(otherEmployer, NewJob());
        using var otherForm = ApplicationTests.Form();
        using var otherCreated = await candidate.Client.PostAsync($"/api/jobs/{otherJob.Id}/applications", otherForm);
        Assert.Equal(HttpStatusCode.Created, otherCreated.StatusCode);

        var unused = await app.AccountAsync("Candidate");
        string route = $"/api/admin/users/{unused.Id}";
        await ErrorAsync(await app.Public.DeleteAsync(route), HttpStatusCode.Unauthorized);
        await ErrorAsync(await employer.Client.DeleteAsync(route), HttpStatusCode.Forbidden);
        await ErrorAsync(await app.Admin.Client.DeleteAsync("/api/admin/users/2147483647"), HttpStatusCode.NotFound);
        await ErrorAsync(await app.Admin.Client.DeleteAsync($"/api/admin/users/{app.Admin.Id}"), HttpStatusCode.Forbidden);

        await ErrorAsync(await app.Admin.Client.DeleteAsync($"/api/admin/users/{candidate.Id}"), HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.DeleteAsync($"/api/admin/users/{employer.Id}")).StatusCode);
        await ErrorAsync(await candidate.Client.GetAsync(created.Headers.Location!), HttpStatusCode.NotFound);
        await ErrorAsync(await app.Admin.Client.GetAsync($"/api/jobs/{job.Id}"), HttpStatusCode.NotFound);
        var removedJobApplications = await ReadAsync<PagedResult<ApplicationResponse>>(await candidate.Client.GetAsync(
            $"/api/applications/mine?jobId={job.Id}"));
        Assert.Empty(removedJobApplications.Items);
        var preservedApplications = await ReadAsync<PagedResult<ApplicationResponse>>(await candidate.Client.GetAsync(
            $"/api/applications/mine?jobId={otherJob.Id}"));
        Assert.Single(preservedApplications.Items);
        await ErrorAsync(await employer.Client.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);

        Assert.Equal(HttpStatusCode.NoContent, (await app.Admin.Client.DeleteAsync(route)).StatusCode);
        var result = await ReadAsync<PagedResult<AccountSummary>>(await app.Admin.Client.GetAsync(
            "/api/admin/users?keyword=" + Uri.EscapeDataString(unused.Email)));
        Assert.Empty(result.Items);
        await ErrorAsync(await unused.Client.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);
    }
}
