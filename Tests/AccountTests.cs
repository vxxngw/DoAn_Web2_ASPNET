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
    public async Task Profile_and_account_settings_share_one_page_for_all_roles()
    {
        var candidate = await app.AccountAsync("Candidate");
        var employer = await app.AccountAsync("Employer");
        await RunBrowserAsync("profile-ui.cjs", new { baseUrl = app.Public.BaseAddress, password = Password,
            candidate = candidate.Email, employer = employer.Email, admin = app.Admin.Email });
    }

    [Fact]
    public async Task Password_change_checks_current_password_and_matching_confirmation_before_saving()
    {
        var owner = await app.AccountAsync("Candidate");
        const string route = "/api/auth/me/password";
        const string newPassword = "  New-password-2026!  ";
        var invalidRequests = new[]
        {
            new { currentPassword = "Wrong-password!", newPassword, confirmNewPassword = newPassword },
            new { currentPassword = Password, newPassword, confirmNewPassword = newPassword.Trim() },
            new { currentPassword = Password, newPassword = "short", confirmNewPassword = "short" },
            new { currentPassword = Password, newPassword, confirmNewPassword = "" }
        };
        foreach (var request in invalidRequests)
            await ErrorAsync(await owner.Client.PutAsJsonAsync(route, request), HttpStatusCode.BadRequest);

        var valid = new { currentPassword = Password, newPassword, confirmNewPassword = newPassword };
        using var changed = await owner.Client.PutAsJsonAsync(route, valid);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        await ErrorAsync(await app.Public.PostAsJsonAsync("/api/auth/login", new {owner.Email, password = Password}), HttpStatusCode.Unauthorized);
        var login = await ReadAsync<LoginResponse>(await app.Public.PostAsJsonAsync("/api/auth/login", new {owner.Email, password = newPassword}));
        Assert.Equal(owner.Id, login.User.Id);
        await using var db = app.OpenDb();
        var user = await db.Users.SingleAsync(u => u.Id == owner.Id);
        Assert.NotEqual(newPassword, user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, newPassword));
    }

    [Fact]
    public async Task Two_password_changes_using_the_same_old_password_have_one_winner()
    {
        var owner = await app.AccountAsync("Employer");
        string[] choices = ["Choice-A-123", "Choice-B-123"];
        var responses = await Task.WhenAll(choices.Select(password => owner.Client.PutAsJsonAsync("/api/auth/me/password",
            new { currentPassword = Password, newPassword = password, confirmNewPassword = password })));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);
        for (int i = 0; i < choices.Length; i++)
        {
            using var login = await app.Public.PostAsJsonAsync("/api/auth/login", new { owner.Email, password = choices[i] });
            Assert.Equal(responses[i].IsSuccessStatusCode ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, login.StatusCode);
            responses[i].Dispose();
        }
    }

    [Fact]
    public async Task Personal_avatar_remains_after_login_and_is_scoped_to_its_owner()
    {
        var owner = await app.AccountAsync("Candidate");
        var other = await app.AccountAsync("Candidate");
        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aScsAAAAASUVORK5CYII=";
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Convert.FromBase64String(png)), "image", "avatar.png");
        var updated = await ReadAsync<UserResponse>(await owner.Client.PutAsync("/api/auth/me/avatar", form));
        Assert.Equal("data:image/png;base64," + png, updated.AvatarDataUrl);
        using var invalid = new MultipartFormDataContent();
        invalid.Add(new ByteArrayContent("invalid"u8.ToArray()), "image", "fake.png");
        await ErrorAsync(await owner.Client.PutAsync("/api/auth/me/avatar", invalid), HttpStatusCode.BadRequest);
        using var large = new MultipartFormDataContent();
        large.Add(new ByteArrayContent(new byte[256 * 1024 + 1]), "image", "big.png");
        await ErrorAsync(await owner.Client.PutAsync("/api/auth/me/avatar", large), HttpStatusCode.RequestEntityTooLarge);
        var login = await ReadAsync<LoginResponse>(await app.Public.PostAsJsonAsync("/api/auth/login",new {owner.Email, password = Password}));
        Assert.Equal(updated.AvatarDataUrl, login.User.AvatarDataUrl);
        Assert.Null((await ReadAsync<UserResponse>(await other.Client.GetAsync("/api/auth/me"))).AvatarDataUrl);
        await using var db = app.OpenDb();
        Assert.Equal(Convert.FromBase64String(png), (await db.Users.FindAsync(owner.Id))!.AvatarImage);
    }

    [Fact]
    public async Task Personal_settings_change_only_the_signed_in_profile()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var fields = new {fullName = "Tên hiển thị", companyName = "Mây"};
        await ErrorAsync(await candidate.Client.PutAsJsonAsync("/api/auth/me", fields), HttpStatusCode.BadRequest);
        var saved = await ReadAsync<UserResponse>(await employer.Client.PutAsJsonAsync("/api/auth/me", fields));
        Assert.Equal(employer.Id, saved.Id);
        Assert.Equal(employer.Email, saved.Email);
        Assert.Equal(UserRole.Employer, saved.Role);
        Assert.Equal(fields.fullName, saved.FullName);
        Assert.Equal(fields.companyName, saved.CompanyName);
    }

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
}
