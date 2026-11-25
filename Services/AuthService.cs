using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetUserAsync(int id, CancellationToken ct);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
}

public class AuthService(IUserRepository users, IPasswordHasher<User> hasher, IOptions<JwtSettings> options) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (request.Role is not (UserRole.Candidate or UserRole.Employer))
            throw new BusinessException(400, "Chỉ ứng viên và nhà tuyển dụng được tự đăng ký.");
        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            CompanyName = request.Role == UserRole.Employer ? request.CompanyName!.Trim() : null,
            Role = request.Role!.Value
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        if (!await users.TryAddAsync(user, ct))
            throw new BusinessException(409, "Email đã được đăng ký.");
        return ToResponse(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user == null)
            throw new BusinessException(401, "Email hoặc mật khẩu không đúng.");

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new BusinessException(401, "Email hoặc mật khẩu không đúng.");
        if (user.IsLocked)
            throw new BusinessException(403, "Tài khoản đã bị quản trị viên khóa.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await users.SaveAsync(ct);
        }

        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwt.LifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("role", user.Role.ToString())
        };
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(jwt.SigningKey(), SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, ToResponse(user));
    }

    public async Task<UserResponse> GetUserAsync(int id, CancellationToken ct)
    {
        var user = await users.FindAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");
        return ToResponse(user);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        const string message = "Nếu email tồn tại, liên kết đặt lại mật khẩu đã được tạo.";
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user == null || user.IsLocked)
            return new ForgotPasswordResponse(message, null);

        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("purpose", "password-reset"),
            new Claim("password-version", user.PasswordHash)
        };
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(jwt.SigningKey(), SecurityAlgorithms.HmacSha256));
        return new ForgotPasswordResponse(message, new JwtSecurityTokenHandler().WriteToken(token));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var jwt = options.Value;
        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(request.Token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwt.SigningKey(),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch (SecurityTokenException)
        {
            throw new BusinessException(400, "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (principal.FindFirstValue("purpose") != "password-reset"
            || !int.TryParse(subject, out var userId))
            throw new BusinessException(400, "Liên kết đặt lại mật khẩu không hợp lệ.");

        var user = await users.FindAsync(userId, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");
        if (principal.FindFirstValue("password-version") != user.PasswordHash)
            throw new BusinessException(400, "Liên kết đặt lại mật khẩu đã được sử dụng hoặc không còn hiệu lực.");
        if (user.IsLocked)
            throw new BusinessException(403, "Tài khoản đã bị quản trị viên khóa.");
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        await users.SaveAsync(ct);
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role, user.CompanyName);
}
