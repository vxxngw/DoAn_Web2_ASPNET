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

    [HttpGet("top-companies")]
    public Task<List<TopCompany>> TopCompanies(CancellationToken ct) => jobs.GetTopCompaniesAsync(ct);

    [HttpGet]
    [ProducesResponseType<PagedResult<JobResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<PagedResult<JobResponse>> List([FromQuery] JobQuery query, CancellationToken ct) =>
        jobs.ListAsync(query, null, ct, candidateId: User.IsInRole(nameof(UserRole.Candidate)) ? ViewerId : null);

    [HttpGet("industries")]
    public Task<List<IndustrySummary>> Industries(CancellationToken ct) => jobs.IndustriesAsync(ct);

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

    [HttpGet("{id:int}/jd")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> DownloadJd(int id, CancellationToken ct)
    {
        var pdf = await jobs.GetJdAsync(id, ViewerId, IsAdmin, ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"JD-{id}.pdf\"";
        return File(pdf, "application/pdf");
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPost("with-jd")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType<CreatedResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(413, "application/problem+json")]
    public async Task<ActionResult<CreatedResponse>> CreateWithPdf([FromForm] JobPdfRequest request, CancellationToken ct)
    {
        int id = await jobs.CreateAsync(UserId, request, ct, request.Pdf);
        return CreatedAtAction(nameof(Get), new { id }, new CreatedResponse(id));
    }

    [Authorize(Roles = JobManagers)]
    [HttpPut("{id:int}/with-jd")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(413, "application/problem+json")]
    public async Task<IActionResult> UpdateWithPdf(int id, [FromForm] JobPdfRequest request, CancellationToken ct)
    {
        await jobs.UpdateAsync(id, UserId, request, ct, IsAdmin, request.Pdf);
        return NoContent();
    }

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
