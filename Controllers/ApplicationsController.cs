using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Authorize]
[Route("api/applications")]
public class ApplicationsController(IApplicationService applications) : ApiControllerBase
{
    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("/api/jobs/{jobId:int}/applications")]
    [Consumes("multipart/form-data")]
    [CvUploadLimit]
    [ProducesResponseType<CreatedResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(413, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(415, "application/problem+json")]
    public async Task<ActionResult<CreatedResponse>> Apply(int jobId, [FromForm] ApplyRequest request, CancellationToken ct)
    {
        int id = await applications.ApplyAsync(jobId, UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id }, new CreatedResponse(id));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("admin")]
    [ProducesResponseType<PagedResult<ApplicationResponse>>(200)]
    public Task<PagedResult<ApplicationResponse>> AdminList([FromQuery] ApplicationQuery query, CancellationToken ct) =>
        applications.ListAllAsync(query, ct);

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("mine")]
    [ProducesResponseType<PagedResult<ApplicationResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<ApplicationResponse>> Mine([FromQuery] ApplicationQuery query, CancellationToken ct) =>
        applications.ListMineAsync(UserId, query, ct);

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpGet("/api/jobs/{jobId:int}/applications")]
    [ProducesResponseType<PagedResult<ApplicationResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<PagedResult<ApplicationResponse>> ForJob(int jobId, [FromQuery] ApplicationQuery query, CancellationToken ct) =>
        applications.ListForJobAsync(jobId, UserId, query, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ApplicationResponse>(200)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public Task<ApplicationResponse> Get(int id, CancellationToken ct) =>
        applications.GetAsync(id, UserId, User.IsInRole(nameof(UserRole.Admin)), ct);

    [HttpGet("{id:int}/cv")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> DownloadCv(int id, CancellationToken ct)
    {
        byte[] pdf = await applications.GetCvAsync(id, UserId, User.IsInRole(nameof(UserRole.Admin)), ct);
        Response.Headers.CacheControl = "no-store";
        return File(pdf, "application/pdf", $"cv-{id}.pdf");
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> Review(int id, ReviewRequest request, CancellationToken ct)
    {
        await applications.ReviewAsync(id, UserId, request.Status!.Value, ct);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPatch("{id:int}/withdraw")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> Withdraw(int id, CancellationToken ct)
    {
        await applications.WithdrawAsync(id, UserId, ct);
        return NoContent();
    }
}
