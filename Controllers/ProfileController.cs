using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/profile")]
[Authorize(Roles = nameof(UserRole.Candidate))]
public class ProfileController(IProfileService profiles) : ApiControllerBase
{
    [HttpGet]
    public Task<ProfileResponse> Get(CancellationToken ct) => profiles.GetAsync(UserId, ct);

    [HttpPut]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> Save(CandidateProfileData request, CancellationToken ct)
    {
        await profiles.SaveAsync(UserId, request, ct);
        return NoContent();
    }

    [HttpPut("cv")]
    [Consumes("multipart/form-data")]
    [CvUploadLimit]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(413, "application/problem+json")]
    public async Task<IActionResult> UploadCv([FromForm] CvUploadRequest request, CancellationToken ct)
    {
        await profiles.UploadCvAsync(UserId, request.Cv, ct);
        return NoContent();
    }

    [HttpGet("cv")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> DownloadCv(CancellationToken ct)
    {
        var cv = await profiles.GetCvAsync(UserId, ct);
        Response.Headers.CacheControl = "no-store";
        return File(cv.Content, cv.ContentType, cv.FileName);
    }
}
