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

    [HttpPost("forgot-password")]
    [ProducesResponseType<ForgotPasswordResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    public Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct) =>
        auth.ForgotPasswordAsync(request, ct);

    [HttpPost("reset-password")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(403, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(request, ct);
        return NoContent();
    }
}
