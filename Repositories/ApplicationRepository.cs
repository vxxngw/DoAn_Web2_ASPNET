using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public enum SubmissionResult { Created, JobMissing, JobClosed, AlreadyApplied }

public interface IApplicationRepository
{
    Task<SubmissionResult> SubmitAsync(JobApplication application, CancellationToken ct);
    Task<PagedResult<ApplicationResponse>> ListForCandidateAsync(int candidateId, ApplicationQuery query, CancellationToken ct);
    Task<PagedResult<ApplicationResponse>> ListForJobAsync(int jobId, ApplicationQuery query, CancellationToken ct);
    Task<PagedResult<ApplicationResponse>> ListAllAsync(ApplicationQuery query, CancellationToken ct);
    Task<ApplicationResponse?> GetVisibleAsync(int id, int viewerId, bool isAdmin, CancellationToken ct);
    Task<byte[]?> GetCvAsync(int id, int viewerId, bool isAdmin, CancellationToken ct);
    Task<bool> TryReviewAsync(int id, int employerId, ApplicationStatus status, CancellationToken ct);
    Task<bool> TryWithdrawAsync(int id, int candidateId, CancellationToken ct);
}

public class ApplicationRepository(AppDbContext db) : IApplicationRepository
{
    // Các danh sách chỉ đọc thông tin đơn. Chỉ API tải CV mới đọc cột CvPdf.
    private static readonly Expression<Func<JobApplication, ApplicationResponse>> ToResponse = a =>
        new ApplicationResponse(a.Id, a.JobId, a.Job.Title, a.CandidateId, a.Candidate.FullName,
            a.Candidate.Email, a.Message, a.Status, a.SubmittedAt);

    public async Task<SubmissionResult> SubmitAsync(JobApplication application, CancellationToken ct)
    {
        // Giữ khóa đọc trên tin tới khi ghi đơn xong để chủ tin không thể đóng tin giữa hai bước.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var job = await db.Jobs.AsNoTracking().Where(j => j.Id == application.JobId)
            .Select(j => new { j.IsOpen, j.Deadline, j.Employer.IsLocked }).SingleOrDefaultAsync(ct);
        if (job == null)
            return SubmissionResult.JobMissing;

        application.SubmittedAt = DateTimeOffset.UtcNow;
        if (job.IsLocked || !job.IsOpen || job.Deadline <= application.SubmittedAt)
            return SubmissionResult.JobClosed;

        var existing = await db.Applications
            .Where(a => a.JobId == application.JobId && a.CandidateId == application.CandidateId)
            .SingleOrDefaultAsync(ct);
        if (existing != null)
        {
            if (existing.Status != ApplicationStatus.Withdrawn)
                return SubmissionResult.AlreadyApplied;

            existing.Message = application.Message;
            existing.CvPdf = application.CvPdf;
            existing.Status = ApplicationStatus.Submitted;
            existing.SubmittedAt = application.SubmittedAt;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            application.Id = existing.Id;
            return SubmissionResult.Created;
        }

        db.Applications.Add(application);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return SubmissionResult.Created;
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            return SubmissionResult.AlreadyApplied;
        }
    }

    public Task<PagedResult<ApplicationResponse>> ListForCandidateAsync(int candidateId, ApplicationQuery query, CancellationToken ct) =>
        ListAsync(db.Applications.Where(a => a.CandidateId == candidateId), query, ct);

    public Task<PagedResult<ApplicationResponse>> ListForJobAsync(int jobId, ApplicationQuery query, CancellationToken ct) =>
        ListAsync(db.Applications.Where(a => a.JobId == jobId), query, ct);

    public Task<PagedResult<ApplicationResponse>> ListAllAsync(ApplicationQuery query, CancellationToken ct) =>
        ListAsync(db.Applications, query, ct);

    private static Task<PagedResult<ApplicationResponse>> ListAsync(IQueryable<JobApplication> applications, ApplicationQuery query, CancellationToken ct)
    {
        if (query.JobId.HasValue)
            applications = applications.Where(a => a.JobId == query.JobId.Value);
        if (query.Status.HasValue)
            applications = applications.Where(a => a.Status == query.Status.Value);
        return applications.AsNoTracking().OrderByDescending(a => a.SubmittedAt).ThenByDescending(a => a.Id)
            .Select(ToResponse).ToPageAsync(query, ct);
    }

    private IQueryable<JobApplication> VisibleTo(int viewerId) =>
        db.Applications.AsNoTracking().Where(a => a.CandidateId == viewerId || a.Job.EmployerId == viewerId);

    public Task<ApplicationResponse?> GetVisibleAsync(int id, int viewerId, bool isAdmin, CancellationToken ct)
    {
        var applications = isAdmin ? db.Applications.AsNoTracking() : VisibleTo(viewerId);
        return applications.Where(a => a.Id == id).Select(ToResponse).SingleOrDefaultAsync(ct);
    }

    public Task<byte[]?> GetCvAsync(int id, int viewerId, bool isAdmin, CancellationToken ct)
    {
        var applications = isAdmin ? db.Applications.AsNoTracking() : VisibleTo(viewerId);
        return applications.Where(a => a.Id == id).Select(a => a.CvPdf).SingleOrDefaultAsync(ct);
    }

    public async Task<bool> TryReviewAsync(int id, int employerId, ApplicationStatus status, CancellationToken ct)
    {
        int changed = await db.Applications
            .Where(a => a.Id == id && a.Job.EmployerId == employerId && a.Status == ApplicationStatus.Submitted)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.Status, status), ct);
        return changed == 1;
    }

    public async Task<bool> TryWithdrawAsync(int id, int candidateId, CancellationToken ct)
    {
        int changed = await db.Applications
            .Where(a => a.Id == id && a.CandidateId == candidateId && a.Status == ApplicationStatus.Submitted)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.Status, ApplicationStatus.Withdrawn), ct);
        return changed == 1;
    }
}
