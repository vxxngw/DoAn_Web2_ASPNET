using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Services;

public interface IDashboardService
{
    Task<AdminDashboardStats> GetAdminStatsAsync(CancellationToken ct);
    Task<EmployerDashboardStats> GetEmployerStatsAsync(int employerId, CancellationToken ct);
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<AdminDashboardStats> GetAdminStatsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        int candidates = await db.Users.CountAsync(u => u.Role == UserRole.Candidate, ct);
        int employers = await db.Users.CountAsync(u => u.Role == UserRole.Employer, ct);
        int totalJobs = await db.Jobs.CountAsync(ct);
        int openJobs = await db.Jobs.CountAsync(j => j.IsOpen && j.Deadline >= now, ct);
        int totalApplications = await db.Applications.CountAsync(ct);
        int accepted = await db.Applications.CountAsync(a => a.Status == ApplicationStatus.Accepted, ct);

        var topLocationsData = await db.Jobs
            .GroupBy(j => j.Location)
            .Select(g => new { Location = g.Key, JobCount = g.Count() })
            .OrderByDescending(s => s.JobCount)
            .Take(6)
            .ToListAsync(ct);

        var topLocations = topLocationsData
            .Select(l => new LocationStat(l.Location, l.JobCount))
            .ToList();

        var statuses = await db.Applications
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var statusBreakdown = statuses
            .Select(s => new StatusStat(s.Status.ToString(), s.Count))
            .ToList();

        return new AdminDashboardStats(
            candidates,
            employers,
            totalJobs,
            openJobs,
            totalApplications,
            accepted,
            topLocations,
            statusBreakdown);
    }

    public async Task<EmployerDashboardStats> GetEmployerStatsAsync(int employerId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        int totalJobs = await db.Jobs.CountAsync(j => j.EmployerId == employerId, ct);
        int openJobs = await db.Jobs.CountAsync(j => j.EmployerId == employerId && j.IsOpen && j.Deadline >= now, ct);
        int totalApps = await db.Applications.CountAsync(a => a.Job.EmployerId == employerId, ct);
        int pending = await db.Applications.CountAsync(a => a.Job.EmployerId == employerId && a.Status == ApplicationStatus.Submitted, ct);
        int accepted = await db.Applications.CountAsync(a => a.Job.EmployerId == employerId && a.Status == ApplicationStatus.Accepted, ct);
        int rejected = await db.Applications.CountAsync(a => a.Job.EmployerId == employerId && a.Status == ApplicationStatus.Rejected, ct);

        var topJobsData = await db.Jobs
            .Where(j => j.EmployerId == employerId)
            .Select(j => new
            {
                j.Id,
                j.Title,
                ApplicationCount = db.Applications.Count(a => a.JobId == j.Id),
                IsOpen = j.IsOpen && j.Deadline >= now
            })
            .OrderByDescending(j => j.ApplicationCount)
            .Take(5)
            .ToListAsync(ct);

        var topJobs = topJobsData
            .Select(j => new EmployerJobStat(j.Id, j.Title, j.ApplicationCount, j.IsOpen))
            .ToList();

        return new EmployerDashboardStats(
            totalJobs,
            openJobs,
            totalApps,
            pending,
            accepted,
            rejected,
            topJobs);
    }
}
