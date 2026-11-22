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

    [HttpPatch("users/{id:int}/lock")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> SetLock(int id, AccountLockRequest request, CancellationToken ct)
    {
        await admin.SetLockAsync(id, request.IsLocked!.Value, ct);
        return NoContent();
    }

    [HttpGet("jobs")]
    [ProducesResponseType<PagedResult<JobResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<JobResponse>> AllJobs([FromQuery] JobQuery query, CancellationToken ct) =>
        jobs.ListAsync(query, null, ct, includeAll: true);
}
