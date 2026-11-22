using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IApplicationService
{
    Task<int> ApplyAsync(int jobId, int candidateId, ApplyRequest request, CancellationToken ct);
    Task<PagedResult<ApplicationResponse>> ListMineAsync(int candidateId, ApplicationQuery query, CancellationToken ct);
    Task<PagedResult<ApplicationResponse>> ListForJobAsync(int jobId, int employerId, ApplicationQuery query, CancellationToken ct);
    Task<ApplicationResponse> GetAsync(int id, int viewerId, CancellationToken ct);
    Task<byte[]> GetCvAsync(int id, int viewerId, CancellationToken ct);
    Task ReviewAsync(int id, int employerId, ApplicationStatus status, CancellationToken ct);
}

public class ApplicationService(IApplicationRepository applications, IJobService jobs) : IApplicationService
{
    private const int MaxCvBytes = 5 * 1024 * 1024;

    public async Task<int> ApplyAsync(int jobId, int candidateId, ApplyRequest request, CancellationToken ct)
    {
        var file = request.Cv;
        if (file.Length > MaxCvBytes)
            throw new BusinessException(413, "CV vượt quá 5 MiB.");
        if (file.Length < 5 || !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(400, "Vui lòng chọn tệp CV định dạng PDF.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        var pdf = buffer.ToArray();
        if (!pdf.AsSpan().StartsWith("%PDF-"u8))
            throw new BusinessException(400, "Nội dung tệp thiếu chữ ký PDF.");

        var application = new JobApplication
        {
            JobId = jobId, CandidateId = candidateId, Message = request.Message.Trim(),
            CvPdf = pdf, Status = ApplicationStatus.Submitted
        };
        var result = await applications.SubmitAsync(application, ct);
        return result switch
        {
            SubmissionResult.Created => application.Id,
            SubmissionResult.JobMissing => throw new BusinessException(404, "Không tìm thấy tin."),
            SubmissionResult.JobClosed => throw new BusinessException(409, "Tin đã đóng hoặc hết hạn nhận đơn."),
            SubmissionResult.AlreadyApplied => throw new BusinessException(409, "Bạn đã nộp đơn cho tin này."),
            _ => throw new InvalidOperationException("Kết quả nộp đơn không xác định.")
        };
    }

    public Task<PagedResult<ApplicationResponse>> ListMineAsync(int candidateId, ApplicationQuery query, CancellationToken ct) =>
        applications.ListForCandidateAsync(candidateId, query, ct);

    public async Task<PagedResult<ApplicationResponse>> ListForJobAsync(int jobId, int employerId, ApplicationQuery query, CancellationToken ct)
    {
        await jobs.CheckOwnerAsync(jobId, employerId, ct);
        return await applications.ListForJobAsync(jobId, query, ct);
    }

    public async Task<ApplicationResponse> GetAsync(int id, int viewerId, CancellationToken ct) =>
        await applications.GetVisibleAsync(id, viewerId, ct) ?? throw new BusinessException(404, "Không tìm thấy đơn.");

    public async Task<byte[]> GetCvAsync(int id, int viewerId, CancellationToken ct) =>
        await applications.GetCvAsync(id, viewerId, ct) ?? throw new BusinessException(404, "Không tìm thấy CV.");

    public async Task ReviewAsync(int id, int employerId, ApplicationStatus status, CancellationToken ct)
    {
        if (status != ApplicationStatus.Accepted && status != ApplicationStatus.Rejected)
            throw new BusinessException(400, "Chọn Accepted hoặc Rejected để xử lý đơn.");
        if (!await applications.TryReviewAsync(id, employerId, status, ct))
        {
            await GetAsync(id, employerId, ct);
            throw new BusinessException(409, "Đơn đã có kết quả. Chỉ có thể xử lý đơn mới nộp.");
        }
    }
}
