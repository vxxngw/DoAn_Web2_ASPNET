using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class ProfileTests(ApiFixture app)
{
    private static readonly object[] NoItems = [];

    private static readonly object Valid = new
    {
        yearsOfExperience = 3, currentLevel = "Nhân viên", highestDegree = "Cử nhân", phone = "+84 799 420 628",
        address = "Hải Châu, Đà Nẵng", desiredLocation = "Đà Nẵng", desiredSalary = 15000000,
        experiences = new object[] { new { title = "Backend Developer", company = "Công nghệ Mộc Lan", from = "2024-01-01", to = (string?)null, description = "Xây dựng API" } },
        educations = new object[] { new { school = "Đại học Bách khoa", major = "Công nghệ thông tin", from = "2019-09-01", to = "2023-06-01" } },
        skills = new object[] { new { name = "C#", level = 4 } },
        languages = new object[] { new { name = "Tiếng Anh", level = "Trung cấp" } }
    };

    private static MultipartFormDataContent Cv(byte[] bytes, string fileName) => new() { { new ByteArrayContent(bytes), "cv", fileName } };

    [Fact]
    public async Task Candidate_saves_profile_lists_replaces_them_on_next_save_and_invalid_entries_are_rejected()
    {
        var candidate = await app.AccountAsync("Candidate");
        var empty = await ReadAsync<ProfileResponse>(await candidate.Client.GetAsync("/api/profile"));
        Assert.Empty(empty.Profile.Experiences);
        Assert.Null(empty.Cv);

        using (var saved = await candidate.Client.PutAsJsonAsync("/api/profile", Valid))
            Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        var profile = (await ReadAsync<ProfileResponse>(await candidate.Client.GetAsync("/api/profile"))).Profile;
        Assert.Equal("+84 799 420 628", profile.Phone);
        Assert.Equal(15000000, profile.DesiredSalary);
        var experience = Assert.Single(profile.Experiences);
        Assert.Equal(new DateOnly(2024, 1, 1), experience.From);
        Assert.Null(experience.To);
        Assert.Equal(new DateOnly(2023, 6, 1), Assert.Single(profile.Educations).To);
        Assert.Equal(4, Assert.Single(profile.Skills).Level);
        Assert.Equal("Trung cấp", Assert.Single(profile.Languages).Level);

        // Lần lưu thứ hai đi qua nhánh cập nhật và phải thay toàn bộ danh sách.
        var replaced = new { phone = (string?)null, experiences = NoItems, educations = NoItems, languages = NoItems,
            skills = new object[] { new { name = "SQL", level = 3 }, new { name = "JavaScript", level = 5 } } };
        using (var saved = await candidate.Client.PutAsJsonAsync("/api/profile", replaced))
            Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        profile = (await ReadAsync<ProfileResponse>(await candidate.Client.GetAsync("/api/profile"))).Profile;
        Assert.Null(profile.Phone);
        Assert.Empty(profile.Experiences);
        Assert.Equal(new[] { "SQL", "JavaScript" }, profile.Skills.Select(s => s.Name));

        var invalid = new object[]
        {
            replaced with { experiences = new object[] { new { title = "Dev", company = "A", from = "2024-05-01", to = "2024-01-01" } } },
            replaced with { experiences = new object[] { new { title = "Dev", company = "A" } } },
            replaced with { skills = new object[] { new { name = "C#", level = 6 } } },
            replaced with { phone = "gọi cho tôi" },
            new { experiences = NoItems, educations = NoItems, skills = NoItems, languages = (object[]?)null }
        };
        foreach (var body in invalid)
            await ErrorAsync(await candidate.Client.PutAsJsonAsync("/api/profile", body), HttpStatusCode.BadRequest);

        var employer = await app.AccountAsync("Employer");
        await ErrorAsync(await employer.Client.GetAsync("/api/profile"), HttpStatusCode.Forbidden);
        await ErrorAsync(await employer.Client.PutAsJsonAsync("/api/profile", Valid), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Attached_cv_must_be_a_real_pdf_or_word_file_under_5_MiB_and_is_deleted_with_the_account()
    {
        var candidate = await app.AccountAsync("Candidate");
        await ErrorAsync(await candidate.Client.GetAsync("/api/profile/cv"), HttpStatusCode.NotFound);

        byte[] pdf = ApplicationTests.Pdf;
        using (var uploaded = await candidate.Client.PutAsync("/api/profile/cv", Cv(pdf, "VoQuocVuong_CV.pdf")))
            Assert.Equal(HttpStatusCode.NoContent, uploaded.StatusCode);
        var cv = (await ReadAsync<ProfileResponse>(await candidate.Client.GetAsync("/api/profile"))).Cv!;
        Assert.Equal("VoQuocVuong_CV.pdf", cv.FileName);
        Assert.Equal(pdf.Length, cv.Size);
        using (var download = await candidate.Client.GetAsync("/api/profile/cv"))
        {
            Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());
        }

        byte[] docx = [0x50, 0x4B, 0x03, 0x04, .. new byte[64]];
        using (var replaced = await candidate.Client.PutAsync("/api/profile/cv", Cv(docx, "cv.docx")))
            Assert.Equal(HttpStatusCode.NoContent, replaced.StatusCode);
        using (var download = await candidate.Client.GetAsync("/api/profile/cv"))
        {
            Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", download.Content.Headers.ContentType?.MediaType);
            Assert.Equal(docx, await download.Content.ReadAsByteArrayAsync());
        }

        await ErrorAsync(await candidate.Client.PutAsync("/api/profile/cv", Cv(pdf, "cv.docx")), HttpStatusCode.BadRequest);
        await ErrorAsync(await candidate.Client.PutAsync("/api/profile/cv", Cv("không phải word"u8.ToArray(), "cv.doc")), HttpStatusCode.BadRequest);
        await ErrorAsync(await candidate.Client.PutAsync("/api/profile/cv", Cv(pdf, "cv.txt")), HttpStatusCode.BadRequest);
        byte[] large = [.. "%PDF-"u8, .. new byte[5 * 1024 * 1024]];
        await ErrorAsync(await candidate.Client.PutAsync("/api/profile/cv", Cv(large, "big.pdf")), HttpStatusCode.RequestEntityTooLarge);
        using (var current = await candidate.Client.GetAsync("/api/profile/cv"))
            Assert.Equal(docx, await current.Content.ReadAsByteArrayAsync());

        var employer = await app.AccountAsync("Employer");
        await ErrorAsync(await employer.Client.PutAsync("/api/profile/cv", Cv(pdf, "cv.pdf")), HttpStatusCode.Forbidden);

        using (var saved = await candidate.Client.PutAsJsonAsync("/api/profile", Valid))
            Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        using (var deleted = await app.Admin.Client.DeleteAsync($"/api/admin/users/{candidate.Id}"))
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await using var db = app.OpenDb();
        Assert.False(await db.CandidateProfiles.AnyAsync(p => p.UserId == candidate.Id));
        Assert.False(await db.CandidateCvs.AnyAsync(c => c.UserId == candidate.Id));
    }
}
