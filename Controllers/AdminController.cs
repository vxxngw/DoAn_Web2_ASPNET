using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin")]
public class AdminController(IAdminService admin, IJobService jobs) : ApiControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType<PagedResult<AccountSummary>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<AccountSummary>> Users([FromQuery] AccountQuery filter, CancellationToken ct) =>
        admin.ListUsersAsync(filter, ct);

    [HttpGet("users/{id:int}")]
    [ProducesResponseType<AccountSummary>(200)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<AccountSummary> UserDetails(int id, CancellationToken ct) => admin.GetUserAsync(id, ct);

    [HttpPut("users/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> EditUser(int id, EditAccountRequest request, CancellationToken ct)
    {
        await admin.EditUserAsync(id, request, ct);
        return NoContent();
    }

    [HttpPatch("users/{id:int}/lock")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> SetLock(int id, AccountLockRequest request, CancellationToken ct)
    {
        await admin.SetLockAsync(id, request.IsLocked!.Value, ct);
        return NoContent();
    }

    [HttpGet("users/{id:int}/deletion-impact")]
    [ProducesResponseType<DeletionImpact>(200)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<DeletionImpact> UserImpact(int id, CancellationToken ct) => admin.UserImpactAsync(id, ct);

    [HttpDelete("users/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        await admin.DeleteUserAsync(id, ct);
        return NoContent();
    }

    [HttpGet("jobs/{id:int}/deletion-impact")]
    [ProducesResponseType<DeletionImpact>(200)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<DeletionImpact> JobImpact(int id, CancellationToken ct) => admin.JobImpactAsync(id, ct);

    [HttpGet("statistics")]
    [ProducesResponseType<AdminStatistics>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<AdminStatistics> Statistics([FromQuery] StatisticsQuery query, CancellationToken ct) =>
        admin.StatisticsAsync(query, ct);

    [HttpGet("jobs")]
    [ProducesResponseType<PagedResult<JobResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<JobResponse>> AllJobs([FromQuery] JobQuery query, CancellationToken ct) =>
        jobs.ListAsync(query, null, ct, includeAll: true);
}
