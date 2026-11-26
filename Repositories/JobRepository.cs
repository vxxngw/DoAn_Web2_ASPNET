using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface IJobRepository
{
    Task<EmployerReport> EmployerReportAsync(int employerId, PageQuery page, CancellationToken ct);
    Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct);
    Task<List<TopCompany>> GetTopCompaniesAsync(CancellationToken ct);
    Task<List<IndustrySummary>> IndustriesAsync(CancellationToken ct);
    Task<PagedResult<JobResponse>> ListAsync(JobQuery filter, int? ownerId, CancellationToken ct, bool includeAll = false, int? candidateId = null);
    Task<PagedResult<SavedJobItem>> SavedAsync(int candidateId, PageQuery page, CancellationToken ct);
    Task<bool> SaveJobAsync(int candidateId, int jobId, CancellationToken ct);
    Task RemoveSavedAsync(int candidateId, int jobId, CancellationToken ct);
    Task<JobResponse?> GetVisibleAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false);
    Task<Job?> FindAsync(int id, CancellationToken ct);
    Task AddAsync(Job job, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task RemoveOrArchiveAsync(Job job, CancellationToken ct);
    Task<byte[]?> GetJdAsync(int id, int? viewerId, bool isAdmin, CancellationToken ct);
    Task DeleteWithApplicationsAsync(Job job, CancellationToken ct);
}

public class JobRepository(AppDbContext db) : IJobRepository
{
    public async Task<EmployerReport> EmployerReportAsync(int employerId, PageQuery page, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var jobs = db.Jobs.AsNoTracking().Where(j => j.EmployerId == employerId);
        var totals = await jobs.GroupBy(j => 1).Select(g => new EmployerJobTotals(
            g.Count(j => j.ArchivedAt == null && j.IsOpen && j.Deadline > now), g.Count(j => j.ArchivedAt == null && !j.IsOpen),
            g.Count(j => j.ArchivedAt == null && j.IsOpen && j.Deadline <= now), g.Count(j => j.ArchivedAt != null))).SingleOrDefaultAsync(ct);
        int applications = await db.Applications.CountAsync(a => a.Job.EmployerId == employerId, ct);
        var rows = await jobs.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id)
            .Select(j => new EmployerJobRow(j.Id, j.Title, j.ArchivedAt != null ? "Archived" : !j.IsOpen ? "Closed" : j.Deadline <= now ? "Expired" : "Open",
                j.Deadline, db.Applications.Count(a => a.JobId == j.Id))).ToPageAsync(page, ct);
        return new EmployerReport(applications, totals ?? new EmployerJobTotals(0, 0, 0, 0), rows);
    }

    private IQueryable<Job> PublicJobs(DateTimeOffset now) =>
        db.Jobs.AsNoTracking().Where(j => j.ArchivedAt == null && !j.Employer.IsLocked && j.IsOpen && j.Deadline > now);

    public Task<List<TopCompany>> GetTopCompaniesAsync(CancellationToken ct) =>
        PublicJobs(DateTimeOffset.UtcNow)
            .GroupBy(j => new { j.EmployerId, j.Employer.CompanyName, HasCompanyLogo = j.Employer.CompanyLogo != null })
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key.CompanyName).ThenBy(g => g.Key.EmployerId)
            .Take(4).Select(g => new TopCompany(g.Key.EmployerId, g.Key.CompanyName!, g.Count(), g.Key.HasCompanyLogo))
            .ToListAsync(ct);

    public async Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var since = now.AddDays(-7);
        return await PublicJobs(now)
            .GroupBy(j => 1)
            .Select(g => new JobSummary(g.Count(), g.Select(j => j.EmployerId).Distinct().Count(),
                g.Select(j => j.Location).Distinct().Count(), g.Count(j => j.CreatedAt >= since)))
            .SingleOrDefaultAsync(ct) ?? new JobSummary(0, 0, 0, 0);
    }

    // EF dịch biểu thức Select thành SQL, kể cả tên công ty lấy từ bảng Users.
    private Expression<Func<Job, JobResponse>> ToResponse(int? candidateId) => j =>
        new JobResponse(j.Id, j.EmployerId, j.Employer.CompanyName!, j.Title, j.Description,
            j.Location, j.Deadline, j.IsOpen, j.CreatedAt, j.RowVersion, j.Employer.IsLocked, j.Industry,
            candidateId.HasValue && db.SavedJobs.Any(s => s.JobId == j.Id && s.CandidateId == candidateId),
            j.IsSalaryNegotiable, j.SalaryMin, j.SalaryMax, !j.Employer.IsLocked && j.Employer.CompanyLogo != null,
            j.JdUrl, j.JdPdf != null, j.ArchivedAt != null);

    public Task<List<IndustrySummary>> IndustriesAsync(CancellationToken ct) =>
        PublicJobs(DateTimeOffset.UtcNow).Where(j => j.Industry != null)
            .GroupBy(j => j.Industry).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => new IndustrySummary(g.Key!, g.Count())).ToListAsync(ct);

    public Task<PagedResult<SavedJobItem>> SavedAsync(int candidateId, PageQuery page, CancellationToken ct) =>
        db.SavedJobs.AsNoTracking().Where(s => s.CandidateId == candidateId)
            .OrderByDescending(s => s.SavedAt).ThenByDescending(s => s.JobId)
            .Select(s => new SavedJobItem(s.JobId, s.Job.Title, s.Job.Employer.CompanyName!, s.Job.Location,
                s.Job.Deadline, s.Job.IsOpen, s.Job.Employer.IsLocked, s.SavedAt,
                s.Job.IsSalaryNegotiable, s.Job.SalaryMin, s.Job.SalaryMax, s.Job.EmployerId,
                !s.Job.Employer.IsLocked && s.Job.Employer.CompanyLogo != null)).ToPageAsync(page, ct);

    public async Task<bool> SaveJobAsync(int candidateId, int jobId, CancellationToken ct)
    {
        if (!await PublicJobs(DateTimeOffset.UtcNow).AnyAsync(j => j.Id == jobId, ct)) return false;
        var saved = new SavedJob { CandidateId = candidateId, JobId = jobId, SavedAt = DateTimeOffset.UtcNow };
        db.SavedJobs.Add(saved);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            db.Entry(saved).State = EntityState.Detached;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            // Tin hoặc tài khoản vừa bị xóa giữa lúc kiểm tra và lưu.
            return false;
        }
        return true;
    }

    public Task RemoveSavedAsync(int candidateId, int jobId, CancellationToken ct) =>
        db.SavedJobs.Where(s => s.CandidateId == candidateId && s.JobId == jobId).ExecuteDeleteAsync(ct);

    public Task<PagedResult<JobResponse>> ListAsync(JobQuery filter, int? ownerId, CancellationToken ct, bool includeAll = false, int? candidateId = null)
    {
        var query = db.Jobs.AsNoTracking();
        var now = DateTimeOffset.UtcNow;
        if (ownerId.HasValue)
            query = query.Where(j => j.EmployerId == ownerId.Value && j.ArchivedAt == null);
        else if (!includeAll)
            query = PublicJobs(now);

        if (filter.EmployerId.HasValue)
            query = query.Where(j => j.EmployerId == filter.EmployerId);
        if (!string.IsNullOrWhiteSpace(filter.Industry))
            query = query.Where(j => j.Industry == filter.Industry.Trim());

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(j => j.Title.Contains(keyword) || j.Employer.CompanyName!.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var location = filter.Location.Trim();
            query = query.Where(j => j.Location.Contains(location));
        }

        var sorted = filter.Sort switch
        {
            JobSort.Featured => query.OrderByDescending(j => db.Applications.Count(a => a.JobId == j.Id))
                .ThenByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id),
            JobSort.Title => query.OrderBy(j => j.Title).ThenBy(j => j.Id),
            JobSort.Deadline => query.OrderBy(j => j.Deadline).ThenBy(j => j.Id),
            _ => query.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id)
        };
        return sorted.Select(ToResponse(candidateId)).ToPageAsync(filter, ct);
    }

    private IQueryable<Job> VisibleJob(int id, int? viewerId, bool isAdmin)
    {
        var now = DateTimeOffset.UtcNow;
        return db.Jobs.AsNoTracking()
            .Where(j => j.Id == id && (isAdmin || j.EmployerId == viewerId || (j.ArchivedAt == null && !j.Employer.IsLocked && j.IsOpen && j.Deadline > now)));
    }

    public Task<JobResponse?> GetVisibleAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false) =>
        VisibleJob(id,viewerId,isAdmin).Select(ToResponse(viewerId)).SingleOrDefaultAsync(ct);

    public Task<byte[]?> GetJdAsync(int id, int? viewerId, bool isAdmin, CancellationToken ct) =>
        VisibleJob(id,viewerId,isAdmin).Select(j => j.JdPdf).SingleOrDefaultAsync(ct);

    public Task<Job?> FindAsync(int id, CancellationToken ct) =>
        db.Jobs.SingleOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(Job job, CancellationToken ct)
    {
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task RemoveOrArchiveAsync(Job job, CancellationToken ct)
    {
        if (job.ArchivedAt != null) return;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var posting = db.Jobs.Where(j => j.Id == job.Id);
        // Khóa dòng tin trước khi đếm đơn, cùng thứ tự với giao dịch nộp hồ sơ.
        int updated = await posting.Where(j => j.RowVersion == job.RowVersion)
            .ExecuteUpdateAsync(set => set.SetProperty(j => j.IsOpen, false), ct);
        if (updated == 0) throw new DbUpdateConcurrencyException();
        if (await db.Applications.AnyAsync(a => a.JobId == job.Id, ct))
            await posting.ExecuteUpdateAsync(set => set.SetProperty(j => j.ArchivedAt, DateTimeOffset.UtcNow), ct);
        else
            await posting.ExecuteDeleteAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteWithApplicationsAsync(Job job, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await db.Applications.Where(a => a.JobId == job.Id).ExecuteDeleteAsync(ct);
        db.Jobs.Remove(job);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
