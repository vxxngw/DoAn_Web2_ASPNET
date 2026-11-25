using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/jobs")]
public class JobsController(IJobService jobs, ISavedJobService savedJobs) : ApiControllerBase
{
    private const string JobManagers = nameof(UserRole.Employer) + "," + nameof(UserRole.Admin);
    private bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

    [HttpGet("summary")]
    public Task<JobSummary> Summary(CancellationToken ct) => jobs.GetPublicSummaryAsync(ct);

    [HttpGet]
    [ProducesResponseType<PagedResult<JobResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<JobResponse>> List([FromQuery] JobQuery query, CancellationToken ct) =>
        jobs.ListAsync(query, null, ct);

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpGet("mine")]
    [ProducesResponseType<PagedResult<JobResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<JobResponse>> Mine([FromQuery] JobQuery query, CancellationToken ct) =>
        jobs.ListAsync(query, UserId, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<JobResponse>(200)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<JobResponse> Get(int id, CancellationToken ct) => jobs.GetAsync(id, ViewerId, ct, IsAdmin);

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPost]
    [ProducesResponseType<CreatedResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public async Task<ActionResult<CreatedResponse>> Create(JobRequest request, CancellationToken ct)
    {
        int id = await jobs.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id }, new CreatedResponse(id));
    }

    [Authorize(Roles = JobManagers)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> Update(int id, UpdateJobRequest request, CancellationToken ct)
    {
        await jobs.UpdateAsync(id, UserId, request, ct, IsAdmin);
        return NoContent();
    }

    [Authorize(Roles = JobManagers)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await jobs.DeleteAsync(id, UserId, ct, IsAdmin);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("saved")]
    [ProducesResponseType<PagedResult<SavedJobResponse>>(200)]
    public Task<PagedResult<SavedJobResponse>> ListSaved([FromQuery] PageQuery query, CancellationToken ct) =>
        savedJobs.ListSavedJobsAsync(UserId, query, ct);

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("saved-ids")]
    public Task<List<int>> GetSavedIds([FromQuery] List<int> ids, CancellationToken ct) =>
        savedJobs.GetSavedJobIdsAsync(UserId, ids, ct);

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("{id:int}/is-saved")]
    public async Task<ActionResult<object>> CheckSaved(int id, CancellationToken ct) =>
        Ok(new { isSaved = await savedJobs.IsSavedAsync(UserId, id, ct) });

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("{id:int}/save")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> Save(int id, CancellationToken ct)
    {
        await savedJobs.SaveJobAsync(UserId, id, ct);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpDelete("{id:int}/save")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Unsave(int id, CancellationToken ct)
    {
        await savedJobs.UnsaveJobAsync(UserId, id, ct);
        return NoContent();
    }

    [Authorize(Roles = JobManagers)]
    [HttpGet("{id:int}/applications/export")]
    [ProducesResponseType(200)]
    [ProducesResponseType<ProblemDetails>(403, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> ExportApplications(int id, [FromServices] IApplicationExportService exporter, CancellationToken ct)
    {
        var (bytes, fileName) = await exporter.ExportApplicationsCsvAsync(id, UserId, IsAdmin, ct);
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}
