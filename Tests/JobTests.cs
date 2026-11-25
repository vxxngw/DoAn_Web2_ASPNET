using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.False(result.HasNext);
        Assert.Equal(2, result.PageNumber);

        // Kiểm tra phân trang với tham số pageNumber
        var page1Result = await ReadAsync<PagedResult<JobResponse>>(await app.Public.GetAsync(
            $"/api/jobs?keyword={prefix}&location=Hu%E1%BA%BF&sort=Title&pageSize=1&pageNumber=1"));
        Assert.Equal(prefix + "A", Assert.Single(page1Result.Items).Title);
        Assert.False(page1Result.HasPrevious);
        Assert.True(page1Result.HasNext);

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

    [Fact]
    public async Task Candidate_can_save_jobs_view_saved_list_and_unsave()
    {
        var employer = await app.AccountAsync("Employer");
        var job = await CreateJobAsync(employer, NewJob("Lập trình viên AI", "Hà Nội"));
        var candidate = await app.AccountAsync("Candidate");

        // Ban đầu chưa lưu
        var check1 = await ReadAsync<JsonElement>(await candidate.Client.GetAsync($"/api/jobs/{job.Id}/is-saved"));
        Assert.False(check1.GetProperty("isSaved").GetBoolean());

        // Lưu tin
        var saveResp = await candidate.Client.PostAsync($"/api/jobs/{job.Id}/save", null);
        Assert.Equal(HttpStatusCode.NoContent, saveResp.StatusCode);

        // Kiểm tra lại trạng thái đã lưu
        var check2 = await ReadAsync<JsonElement>(await candidate.Client.GetAsync($"/api/jobs/{job.Id}/is-saved"));
        Assert.True(check2.GetProperty("isSaved").GetBoolean());

        // Kiểm tra nhiều tin cùng lúc cho danh sách công việc
        var savedIds = await ReadAsync<List<int>>(await candidate.Client.GetAsync($"/api/jobs/saved-ids?ids={job.Id}&ids=999999"));
        Assert.Contains(job.Id, savedIds);
        Assert.DoesNotContain(999999, savedIds);

        // Lấy danh sách việc làm đã lưu
        var list = await ReadAsync<PagedResult<SavedJobResponse>>(await candidate.Client.GetAsync("/api/jobs/saved"));
        Assert.True(list.TotalCount >= 1);
        Assert.Contains(list.Items, s => s.JobId == job.Id);

        // Bỏ lưu tin
        var unsaveResp = await candidate.Client.DeleteAsync($"/api/jobs/{job.Id}/save");
        Assert.Equal(HttpStatusCode.NoContent, unsaveResp.StatusCode);

        // Sau khi bỏ lưu
        var check3 = await ReadAsync<JsonElement>(await candidate.Client.GetAsync($"/api/jobs/{job.Id}/is-saved"));
        Assert.False(check3.GetProperty("isSaved").GetBoolean());
    }

    [Fact]
    public async Task Employer_and_admin_can_export_applications_to_csv()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob());

        using var form = ApplicationTests.Form(message: "Em chào công ty");
        var submitResp = await candidate.Client.PostAsync($"/api/jobs/{job.Id}/applications", form);
        Assert.Equal(HttpStatusCode.Created, submitResp.StatusCode);

        // Employer xuất CSV
        var exportResp = await employer.Client.GetAsync($"/api/jobs/{job.Id}/applications/export");
        Assert.Equal(HttpStatusCode.OK, exportResp.StatusCode);
        Assert.Equal("text/csv; charset=utf-8", exportResp.Content.Headers.ContentType?.ToString());
        var bytes = await exportResp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 3);
        // Có BOM UTF-8
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
        string csvText = System.Text.Encoding.UTF8.GetString(bytes[3..]);
        Assert.Contains("Họ và tên ứng viên", csvText);
        Assert.Contains(candidate.Email, csvText);

        // Admin cũng có quyền xuất
        var adminExport = await app.Admin.Client.GetAsync($"/api/jobs/{job.Id}/applications/export");
        Assert.Equal(HttpStatusCode.OK, adminExport.StatusCode);

        // NTD khác không có quyền
        var otherEmployer = await app.AccountAsync("Employer");
        await ErrorAsync(await otherEmployer.Client.GetAsync($"/api/jobs/{job.Id}/applications/export"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_and_employer_can_view_dashboard_stats()
    {
        var employer = await app.AccountAsync("Employer");
        var adminStats = await ReadAsync<AdminDashboardStats>(await app.Admin.Client.GetAsync("/api/dashboard/admin"));
        Assert.True(adminStats.TotalCandidates >= 0);
        Assert.True(adminStats.TotalJobs >= 0);

        var empStats = await ReadAsync<EmployerDashboardStats>(await employer.Client.GetAsync("/api/dashboard/employer"));
        Assert.True(empStats.TotalJobs >= 0);

        // Người dùng vãng lai không được phép
        await ErrorAsync(await app.Public.GetAsync("/api/dashboard/admin"), HttpStatusCode.Unauthorized);
        await ErrorAsync(await app.Public.GetAsync("/api/dashboard/employer"), HttpStatusCode.Unauthorized);
    }
}
