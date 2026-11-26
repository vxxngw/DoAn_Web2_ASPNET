using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UserResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        StatusCode(201, await auth.RegisterAsync(request, ct));

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(401, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(403, "application/problem+json")]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    [Authorize]
    [HttpGet("me")]
    public Task<UserResponse> Me(CancellationToken ct) => auth.GetUserAsync(UserId, ct);

    [Authorize(Roles = nameof(Recruitment.Api.Models.UserRole.Candidate))]
    [HttpPut("me/candidate-profile")]
    public Task<UserResponse> CandidateProfile(CandidateProfileRequest request, CancellationToken ct) =>
        auth.UpdateCandidateProfileAsync(UserId, request, ct);

    [Authorize]
    [HttpPut("me/password")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(UserId, request, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType<UserResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<UserResponse> UpdateMe(PersonalAccountRequest request, CancellationToken ct) =>
        auth.UpdateOwnDetailsAsync(UserId, request, ct);

    [Authorize]
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(384 * 1024)]
    [ProducesResponseType<UserResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(413, "application/problem+json")]
    public Task<UserResponse> Avatar([FromForm] ImageUploadRequest request, CancellationToken ct) =>
        auth.UpdateAvatarAsync(UserId, request.Image, ct);
}
