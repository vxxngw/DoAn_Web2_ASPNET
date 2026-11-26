using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Authorize(Roles = nameof(UserRole.Employer))]
[Route("api/employer")]
public class EmployerController(IJobService jobs, IApplicationService applications, IAuthService accounts) : ApiControllerBase
{
    [HttpGet("applications")]
    public Task<PagedResult<ApplicationResponse>> Applications([FromQuery] ApplicationQuery query, CancellationToken ct) =>
        applications.ListReceivedAsync(UserId, query, ct);

    [HttpGet("candidates")]
    public Task<PagedResult<PublicCandidate>> Candidates([FromQuery] CandidateSearch query, CancellationToken ct) =>
        accounts.PublicCandidatesAsync(query, ct);

    [HttpGet("candidates/{id:int}")]
    [ProducesResponseType<PublicCandidate>(200)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<PublicCandidate> Candidate(int id, CancellationToken ct) => accounts.PublicCandidateAsync(id, ct);

    [HttpGet("report")]
    public Task<EmployerReport> Report([FromQuery] PageQuery query, CancellationToken ct) =>
        jobs.EmployerReportAsync(UserId, query, ct);
}
