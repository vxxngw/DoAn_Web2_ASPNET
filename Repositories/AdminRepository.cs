using System.Data;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface IAdminRepository
{
    Task<DeletionImpact?> UserImpactAsync(int id, CancellationToken ct);
    Task<DeletionImpact?> JobImpactAsync(int id, CancellationToken ct);
    Task<bool> DeleteUserAsync(int id, CancellationToken ct);
    Task<AdminStatistics> StatisticsAsync(ReportPeriod period, DateOnly? start, DateOnly? endExclusive, CancellationToken ct);
}

public class AdminRepository(AppDbContext db) : IAdminRepository
{
    public Task<DeletionImpact?> UserImpactAsync(int id, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id).Select(u => new DeletionImpact(
            db.Jobs.Count(j => j.EmployerId == u.Id),
            db.Applications.Count(a => a.CandidateId == u.Id || a.Job.EmployerId == u.Id))).SingleOrDefaultAsync(ct);

    public Task<DeletionImpact?> JobImpactAsync(int id, CancellationToken ct) =>
        db.Jobs.Where(j => j.Id == id).Select(j => new DeletionImpact(1,
            db.Applications.Count(a => a.JobId == j.Id))).SingleOrDefaultAsync(ct);

    public async Task<bool> DeleteUserAsync(int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var account = db.Users.Where(u => u.Id == id && u.Role != UserRole.Admin);
        if (!await account.AnyAsync(ct))
            return false;
        await db.Applications.Where(a => a.CandidateId == id || a.Job.EmployerId == id).ExecuteDeleteAsync(ct);
        await db.Jobs.Where(j => j.EmployerId == id).ExecuteDeleteAsync(ct);
        await account.ExecuteDeleteAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<AdminStatistics> StatisticsAsync(ReportPeriod period, DateOnly? start, DateOnly? endExclusive, CancellationToken ct)
    {
        var jobs = db.Jobs.AsQueryable();
        var submitted = db.Applications.AsQueryable();
        var reviews = db.Applications.Where(a => a.Status != ApplicationStatus.Submitted);
        if (start.HasValue && endExclusive.HasValue)
        {
            var from = new DateTimeOffset(start.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
            var until = new DateTimeOffset(endExclusive.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
            jobs = jobs.Where(j => j.CreatedAt >= from && j.CreatedAt < until);
            submitted = submitted.Where(a => a.SubmittedAt >= from && a.SubmittedAt < until);
            reviews = reviews.Where(a => a.ReviewedAt >= from && a.ReviewedAt < until);
        }
        var totals = await reviews.GroupBy(a => 1).Select(g => new
        {
            Accepted = g.Count(a => a.Status == ApplicationStatus.Accepted),
            Rejected = g.Count(a => a.Status == ApplicationStatus.Rejected),
            WithoutDate = g.Count(a => a.ReviewedAt == null)
        }).SingleOrDefaultAsync(ct);
        return new AdminStatistics(period, start, endExclusive?.AddDays(-1),
            await jobs.CountAsync(ct), await submitted.CountAsync(ct),
            totals?.Accepted ?? 0, totals?.Rejected ?? 0, totals?.WithoutDate ?? 0);
    }
}
