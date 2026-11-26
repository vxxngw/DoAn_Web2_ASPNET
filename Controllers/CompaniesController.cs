using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/companies")]
public class CompaniesController(IAuthService accounts) : ApiControllerBase
{
    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPut("me/logo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1152 * 1024)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(413, "application/problem+json")]
    public async Task<IActionResult> UploadLogo([FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        await accounts.UpdateCompanyLogoAsync(UserId, request.Image, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/logo")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<FileContentResult>(200, "image/jpeg", "image/png")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<IActionResult> Logo(int id, CancellationToken ct)
    {
        var image = await accounts.GetCompanyLogoAsync(id, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Bytes, image.ContentType);
    }
}
