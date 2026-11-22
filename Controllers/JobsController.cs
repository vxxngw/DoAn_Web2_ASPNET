using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/jobs")]
public class JobsController(IJobService jobs) : ApiControllerBase
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
}
