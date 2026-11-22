using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IJobService
{
    Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct);
    Task<PagedResult<JobResponse>> ListAsync(JobQuery query, int? ownerId, CancellationToken ct, bool includeAll = false);
    Task<JobResponse> GetAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false);
    Task<int> CreateAsync(int employerId, JobRequest request, CancellationToken ct);
    Task UpdateAsync(int id, int employerId, UpdateJobRequest request, CancellationToken ct, bool isAdmin = false);
    Task DeleteAsync(int id, int employerId, CancellationToken ct, bool isAdmin = false);
    Task CheckOwnerAsync(int id, int employerId, CancellationToken ct);
}

public class JobService(IJobRepository jobs) : IJobService
{
    public Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct) => jobs.GetPublicSummaryAsync(ct);

    public Task<PagedResult<JobResponse>> ListAsync(JobQuery query, int? ownerId, CancellationToken ct, bool includeAll = false) =>
        jobs.ListAsync(query, ownerId, ct, includeAll);

    public async Task<JobResponse> GetAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false) =>
        await jobs.GetVisibleAsync(id, viewerId, ct, isAdmin) ?? throw new BusinessException(404, "Không tìm thấy tin.");

    public async Task<int> CreateAsync(int employerId, JobRequest request, CancellationToken ct)
    {
        var job = new Job { EmployerId = employerId, CreatedAt = DateTimeOffset.UtcNow };
        SetFields(job, request);
        await jobs.AddAsync(job, ct);
        return job.Id;
    }

    public async Task UpdateAsync(int id, int employerId, UpdateJobRequest request, CancellationToken ct, bool isAdmin = false)
    {
        var job = await GetForManagementAsync(id, employerId, ct, isAdmin);
        if (request.RowVersion != null && !request.RowVersion.SequenceEqual(job.RowVersion))
            throw new BusinessException(409, "Tin đã được sửa ở một phiên khác. Hãy tải lại trước khi lưu.");
        SetFields(job, request);
        await jobs.SaveAsync(ct);
    }

    public async Task DeleteAsync(int id, int employerId, CancellationToken ct, bool isAdmin = false)
    {
        var job = await GetForManagementAsync(id, employerId, ct, isAdmin);
        if (!await jobs.TryDeleteAsync(job, ct))
            throw new BusinessException(409, "Tin đã có đơn ứng tuyển. Bạn có thể đóng tin để dừng nhận đơn.");
    }

    public Task CheckOwnerAsync(int id, int employerId, CancellationToken ct) => GetForManagementAsync(id, employerId, ct, isAdmin: false);

    private async Task<Job> GetForManagementAsync(int id, int employerId, CancellationToken ct, bool isAdmin)
    {
        var job = await jobs.FindAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tin.");
        if (!isAdmin && job.EmployerId != employerId)
            throw new BusinessException(403, "Bạn không có quyền quản lý tin này.");
        return job;
    }

    private static void SetFields(Job job, JobRequest request)
    {
        var deadline = request.Deadline!.Value.ToUniversalTime();
        if (request.IsOpen && deadline <= DateTimeOffset.UtcNow)
            throw new BusinessException(400, "Hạn nộp của tin đang mở phải ở tương lai.");
        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
        job.Location = request.Location.Trim();
        job.Deadline = deadline;
        job.IsOpen = request.IsOpen;
    }
}
