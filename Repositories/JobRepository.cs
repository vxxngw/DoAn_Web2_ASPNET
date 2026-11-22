using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface IJobRepository
{
    Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct);
    Task<PagedResult<JobResponse>> ListAsync(JobQuery filter, int? ownerId, CancellationToken ct, bool includeAll = false);
    Task<JobResponse?> GetVisibleAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false);
    Task<Job?> FindAsync(int id, CancellationToken ct);
    Task AddAsync(Job job, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task<bool> TryDeleteAsync(Job job, CancellationToken ct);
}

public class JobRepository(AppDbContext db) : IJobRepository
{
    public async Task<JobSummary> GetPublicSummaryAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var since = now.AddDays(-7);
        return await db.Jobs.Where(j => !j.Employer.IsLocked && j.IsOpen && j.Deadline > now)
            .GroupBy(j => 1)
            .Select(g => new JobSummary(g.Count(), g.Select(j => j.EmployerId).Distinct().Count(),
                g.Select(j => j.Location).Distinct().Count(), g.Count(j => j.CreatedAt >= since)))
            .SingleOrDefaultAsync(ct) ?? new JobSummary(0, 0, 0, 0);
    }

    // EF dịch biểu thức Select thành SQL, kể cả tên công ty lấy từ bảng Users.
    private static readonly Expression<Func<Job, JobResponse>> ToResponse = j =>
        new JobResponse(j.Id, j.EmployerId, j.Employer.CompanyName!, j.Title, j.Description,
            j.Location, j.Deadline, j.IsOpen, j.CreatedAt, j.RowVersion, j.Employer.IsLocked);

    public Task<PagedResult<JobResponse>> ListAsync(JobQuery filter, int? ownerId, CancellationToken ct, bool includeAll = false)
    {
        var query = db.Jobs.AsNoTracking();
        var now = DateTimeOffset.UtcNow;
        if (ownerId.HasValue)
            query = query.Where(j => j.EmployerId == ownerId.Value);
        else if (!includeAll)
            query = query.Where(j => !j.Employer.IsLocked && j.IsOpen && j.Deadline > now);

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
            JobSort.Title => query.OrderBy(j => j.Title).ThenBy(j => j.Id),
            JobSort.Deadline => query.OrderBy(j => j.Deadline).ThenBy(j => j.Id),
            _ => query.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id)
        };
        return sorted.Select(ToResponse).ToPageAsync(filter, ct);
    }

    public Task<JobResponse?> GetVisibleAsync(int id, int? viewerId, CancellationToken ct, bool isAdmin = false)
    {
        var now = DateTimeOffset.UtcNow;
        return db.Jobs.AsNoTracking()
            .Where(j => j.Id == id && (isAdmin || j.EmployerId == viewerId || (!j.Employer.IsLocked && j.IsOpen && j.Deadline > now)))
            .Select(ToResponse).SingleOrDefaultAsync(ct);
    }

    public Task<Job?> FindAsync(int id, CancellationToken ct) =>
        db.Jobs.SingleOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(Job job, CancellationToken ct)
    {
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<bool> TryDeleteAsync(Job job, CancellationToken ct)
    {
        db.Jobs.Remove(job);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            // Khóa ngoại giữ lại tin đã có đơn, kể cả khi vừa có người nộp đồng thời.
            return false;
        }
    }
}
