using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/saved-jobs")]
[Authorize(Roles = nameof(UserRole.Candidate))]
public class SavedJobsController(IJobService jobs) : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<SavedJobItem>> List([FromQuery] PageQuery page, CancellationToken ct) =>
        jobs.SavedAsync(UserId, page, ct);

    [HttpPut("{jobId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> Save(int jobId, CancellationToken ct)
    {
        await jobs.SaveJobAsync(UserId, jobId, ct);
        return NoContent();
    }

    [HttpDelete("{jobId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Remove(int jobId, CancellationToken ct)
    {
        await jobs.RemoveSavedAsync(UserId, jobId, ct);
        return NoContent();
    }
}
