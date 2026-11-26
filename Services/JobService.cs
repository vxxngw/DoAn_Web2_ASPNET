using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IJobService
{
    Task<EmployerReport> EmployerReportAsync(int employerId, PageQuery page, CancellationToken ct);
    Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct);
    Task<List<TopCompany>> GetTopCompaniesAsync(CancellationToken ct);
    Task<List<IndustrySummary>> IndustriesAsync(CancellationToken ct);
    Task<PagedResult<JobResponse>> ListAsync(JobQuery query, int? ownerId, CancellationToken ct, bool includeAll = false, int? candidateId = null);
    Task<PagedResult<SavedJobItem>> SavedAsync(int candidateId, PageQuery page, CancellationToken ct);
    Task SaveJobAsync(int candidateId, int jobId, CancellationToken ct);
    Task RemoveSavedAsync(int candidateId, int jobId, CancellationToken ct);
    Task<JobResponse> GetAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false);
    Task<byte[]> GetJdAsync(int id, int? viewerId, bool isAdmin, CancellationToken ct);
    Task<int> CreateAsync(int employerId, JobRequest request, CancellationToken ct, IFormFile? pdf = null);
    Task UpdateAsync(int id, int employerId, UpdateJobRequest request, CancellationToken ct, bool isAdmin = false, IFormFile? pdf = null);
    Task DeleteAsync(int id, int employerId, CancellationToken ct, bool isAdmin = false);
    Task CheckOwnerAsync(int id, int employerId, CancellationToken ct);
}

public class JobService(IJobRepository jobs) : IJobService
{
    public Task<EmployerReport> EmployerReportAsync(int employerId, PageQuery page, CancellationToken ct) =>
        jobs.EmployerReportAsync(employerId, page, ct);

    public Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct) => jobs.GetPublicSummaryAsync(ct);
    public Task<List<TopCompany>> GetTopCompaniesAsync(CancellationToken ct) => jobs.GetTopCompaniesAsync(ct);

    public Task<List<IndustrySummary>> IndustriesAsync(CancellationToken ct) => jobs.IndustriesAsync(ct);

    public Task<PagedResult<JobResponse>> ListAsync(JobQuery query, int? ownerId, CancellationToken ct, bool includeAll = false, int? candidateId = null) =>
        jobs.ListAsync(query, ownerId, ct, includeAll, candidateId);

    public Task<PagedResult<SavedJobItem>> SavedAsync(int candidateId, PageQuery page, CancellationToken ct) =>
        jobs.SavedAsync(candidateId, page, ct);

    public async Task SaveJobAsync(int candidateId, int jobId, CancellationToken ct)
    {
        if (!await jobs.SaveJobAsync(candidateId, jobId, ct))
            throw new BusinessException(404, "Không tìm thấy tin đang nhận hồ sơ.");
    }

    public Task RemoveSavedAsync(int candidateId, int jobId, CancellationToken ct) =>
        jobs.RemoveSavedAsync(candidateId, jobId, ct);

    public async Task<JobResponse> GetAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false) =>
        await jobs.GetVisibleAsync(id, viewerId, ct, isAdmin) ?? throw new BusinessException(404, "Không tìm thấy tin.");

    public async Task<byte[]> GetJdAsync(int id, int? viewerId, bool isAdmin, CancellationToken ct) =>
        await jobs.GetJdAsync(id, viewerId, isAdmin, ct) ?? throw new BusinessException(404, "Không tìm thấy tệp JD.");

    public async Task<int> CreateAsync(int employerId, JobRequest request, CancellationToken ct, IFormFile? pdf = null)
    {
        var job = new Job { EmployerId = employerId, CreatedAt = DateTimeOffset.UtcNow };
        SetFields(job, request);
        await UpdateJdAsync(job, request, pdf, ct);
        await jobs.AddAsync(job, ct);
        return job.Id;
    }

    public async Task UpdateAsync(int id, int employerId, UpdateJobRequest request, CancellationToken ct, bool isAdmin = false, IFormFile? pdf = null)
    {
        var job = await GetForManagementAsync(id, employerId, ct, isAdmin);
        if (job.ArchivedAt != null && !isAdmin)
            throw new BusinessException(409, "Tin đã lưu trữ. Bạn có thể xem lại trong báo cáo.");
        if (request.RowVersion != null && !request.RowVersion.SequenceEqual(job.RowVersion))
            throw new BusinessException(409, "Tin đã được sửa ở một phiên khác. Hãy tải lại trước khi lưu.");
        if (job.ArchivedAt != null && request.IsOpen)
            throw new BusinessException(409, "Tin đã lưu trữ không thể mở nhận đơn.");
        SetFields(job, request);
        await UpdateJdAsync(job, request, pdf, ct);
        await jobs.SaveAsync(ct);
    }

    public async Task DeleteAsync(int id, int employerId, CancellationToken ct, bool isAdmin = false)
    {
        var job = await GetForManagementAsync(id, employerId, ct, isAdmin);
        if (isAdmin)
        {
            await jobs.DeleteWithApplicationsAsync(job, ct);
            return;
        }
        await jobs.RemoveOrArchiveAsync(job, ct);
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
        if (request.IsSalaryNegotiable.HasValue)
        {
            job.IsSalaryNegotiable = request.IsSalaryNegotiable;
            job.SalaryMin = request.SalaryMin;
            job.SalaryMax = request.SalaryMax;
        }
        if (request.Industry != null)
            job.Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim();
        job.Deadline = deadline;
        job.IsOpen = request.IsOpen;
    }

    private static async Task UpdateJdAsync(Job job, JobRequest request, IFormFile? pdf, CancellationToken ct)
    {
        bool remove = request is UpdateJobRequest { RemoveJd: true };
        if ((pdf != null && request.JdUrl != null) || (remove && (pdf != null || request.JdUrl != null)))
            throw new BusinessException(400, "Chỉ chọn một thao tác: gắn đường dẫn, tải PDF hoặc gỡ JD.");
        if (!remove && pdf == null && request.JdUrl == null) return;
        job.JdPdf = pdf == null ? null : await PdfUpload.ReadAsync(pdf, "JD", ct);
        job.JdUrl = request.JdUrl?.Trim();
    }
}
