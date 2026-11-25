using Recruitment.Api.DTOs;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface ISavedJobService
{
    Task<PagedResult<SavedJobResponse>> ListSavedJobsAsync(int candidateId, PageQuery query, CancellationToken ct);
    Task<bool> IsSavedAsync(int candidateId, int jobId, CancellationToken ct);
    Task SaveJobAsync(int candidateId, int jobId, CancellationToken ct);
    Task UnsaveJobAsync(int candidateId, int jobId, CancellationToken ct);
    Task<List<int>> GetSavedJobIdsAsync(int candidateId, IEnumerable<int> jobIds, CancellationToken ct);
}

public class SavedJobService(ISavedJobRepository savedJobs) : ISavedJobService
{
    public Task<PagedResult<SavedJobResponse>> ListSavedJobsAsync(int candidateId, PageQuery query, CancellationToken ct) =>
        savedJobs.ListSavedJobsAsync(candidateId, query, ct);

    public Task<bool> IsSavedAsync(int candidateId, int jobId, CancellationToken ct) =>
        savedJobs.IsSavedAsync(candidateId, jobId, ct);

    public async Task SaveJobAsync(int candidateId, int jobId, CancellationToken ct)
    {
        bool ok = await savedJobs.SaveJobAsync(candidateId, jobId, ct);
        if (!ok) throw new BusinessException(404, "Không tìm thấy công việc để lưu.");
    }

    public Task UnsaveJobAsync(int candidateId, int jobId, CancellationToken ct) =>
        savedJobs.UnsaveJobAsync(candidateId, jobId, ct);

    public Task<List<int>> GetSavedJobIdsAsync(int candidateId, IEnumerable<int> jobIds, CancellationToken ct) =>
        savedJobs.GetSavedJobIdsAsync(candidateId, jobIds, ct);
}
