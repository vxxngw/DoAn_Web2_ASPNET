using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface ISavedJobRepository
{
    Task<PagedResult<SavedJobResponse>> ListSavedJobsAsync(int candidateId, PageQuery query, CancellationToken ct);
    Task<bool> IsSavedAsync(int candidateId, int jobId, CancellationToken ct);
    Task<bool> SaveJobAsync(int candidateId, int jobId, CancellationToken ct);
    Task<bool> UnsaveJobAsync(int candidateId, int jobId, CancellationToken ct);
    Task<List<int>> GetSavedJobIdsAsync(int candidateId, IEnumerable<int> jobIds, CancellationToken ct);
}

public class SavedJobRepository(AppDbContext db) : ISavedJobRepository
{
    public async Task<PagedResult<SavedJobResponse>> ListSavedJobsAsync(int candidateId, PageQuery query, CancellationToken ct)
    {
        return await db.SavedJobs
            .AsNoTracking()
            .Where(s => s.CandidateId == candidateId)
            .OrderByDescending(s => s.SavedAt)
            .Select(s => new SavedJobResponse(
                s.Id,
                s.JobId,
                s.Job.Employer.CompanyName ?? s.Job.Employer.FullName,
                s.Job.Title,
                s.Job.Location,
                s.Job.Deadline,
                s.Job.IsOpen,
                s.SavedAt,
                s.Job.MinSalary,
                s.Job.MaxSalary))
            .ToPageAsync(query, ct);
    }

    public Task<bool> IsSavedAsync(int candidateId, int jobId, CancellationToken ct) =>
        db.SavedJobs.AnyAsync(s => s.CandidateId == candidateId && s.JobId == jobId, ct);

    public async Task<bool> SaveJobAsync(int candidateId, int jobId, CancellationToken ct)
    {
        bool jobExists = await db.Jobs.AnyAsync(j => j.Id == jobId, ct);
        if (!jobExists) return false;

        bool alreadySaved = await db.SavedJobs.AnyAsync(s => s.CandidateId == candidateId && s.JobId == jobId, ct);
        if (alreadySaved) return true;

        db.SavedJobs.Add(new SavedJob
        {
            CandidateId = candidateId,
            JobId = jobId,
            SavedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UnsaveJobAsync(int candidateId, int jobId, CancellationToken ct)
    {
        int deleted = await db.SavedJobs
            .Where(s => s.CandidateId == candidateId && s.JobId == jobId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task<List<int>> GetSavedJobIdsAsync(int candidateId, IEnumerable<int> jobIds, CancellationToken ct)
    {
        var idList = jobIds.ToList();
        if (idList.Count == 0) return [];

        return await db.SavedJobs
            .Where(s => s.CandidateId == candidateId && idList.Contains(s.JobId))
            .Select(s => s.JobId)
            .ToListAsync(ct);
    }
}
