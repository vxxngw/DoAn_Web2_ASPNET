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
    Task<UserResponse> UpdateCandidateProfileAsync(int id, CandidateProfileRequest request, CancellationToken ct);
    Task<PagedResult<PublicCandidate>> PublicCandidatesAsync(CandidateSearch filter, CancellationToken ct);
    Task<PublicCandidate> PublicCandidateAsync(int id, CancellationToken ct);
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task ChangePasswordAsync(int id, ChangePasswordRequest request, CancellationToken ct);
    Task<UserResponse> GetUserAsync(int id, CancellationToken ct);
    Task<UserResponse> UpdateOwnDetailsAsync(int id, PersonalAccountRequest request, CancellationToken ct);
    Task<UserResponse> UpdateAvatarAsync(int id, IFormFile image, CancellationToken ct);
    Task UpdateCompanyLogoAsync(int id, IFormFile image, CancellationToken ct);
    Task<(byte[] Bytes, string ContentType)> GetCompanyLogoAsync(int id, CancellationToken ct);
}

public class AuthService(IUserRepository users, IPasswordHasher<User> hasher, IOptions<JwtSettings> options) : IAuthService
{
    public async Task<UserResponse> UpdateCandidateProfileAsync(int id, CandidateProfileRequest request, CancellationToken ct)
    {
        request.ProfessionalTitle = request.ProfessionalTitle?.Trim();
        request.ProfileSummary = request.ProfileSummary?.Trim();
        if (await users.UpdateCandidateProfileAsync(id,request,ct) == 0) throw new BusinessException(403, "Tài khoản không còn hoạt động.");
        return await GetUserAsync(id,ct);
    }

    public Task<PagedResult<PublicCandidate>> PublicCandidatesAsync(CandidateSearch filter, CancellationToken ct) =>
        users.PublicCandidatesAsync(filter,ct);

    public async Task<PublicCandidate> PublicCandidateAsync(int id, CancellationToken ct) =>
        await users.PublicCandidateAsync(id,ct) ?? throw new BusinessException(404, "Hồ sơ hiện không công khai.");
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
            await SaveNewPasswordAsync(user, request.Password, ct);

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

    public async Task ChangePasswordAsync(int id, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new BusinessException(400, "Mật khẩu hiện tại không đúng.");
        await SaveNewPasswordAsync(user, request.NewPassword, ct);
    }

    private async Task SaveNewPasswordAsync(User user, string password, CancellationToken ct)
    {
        string newHash = hasher.HashPassword(user, password);
        if (!await users.TryChangePasswordHashAsync(user.Id, user.PasswordHash, newHash, ct))
            throw new BusinessException(409, "Thông tin đăng nhập vừa thay đổi. Vui lòng thử lại.");
    }

    public async Task<UserResponse> GetUserAsync(int id, CancellationToken ct)
    {
        var user = await users.FindAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");
        return ToResponse(user);
    }

    public async Task<UserResponse> UpdateOwnDetailsAsync(int id, PersonalAccountRequest request, CancellationToken ct)
    {
        var user = await users.FindAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");
        string? company = request.CompanyName?.Trim();
        if (user.Role == UserRole.Employer && string.IsNullOrEmpty(company))
            throw new BusinessException(400, "Nhà tuyển dụng phải có tên công ty.");
        if (user.Role != UserRole.Employer && !string.IsNullOrEmpty(company))
            throw new BusinessException(400, "Chỉ nhà tuyển dụng có tên công ty.");
        if (await users.UpdateOwnDetailsAsync(id, request.FullName.Trim(), user.Role == UserRole.Employer ? company : null, ct) == 0)
            throw new BusinessException(403, "Tài khoản không còn hoạt động.");
        return await GetUserAsync(id, ct);
    }

    public async Task<UserResponse> UpdateAvatarAsync(int id, IFormFile image, CancellationToken ct)
    {
        var bytes = await ReadImageAsync(image, 256 * 1024, "Ảnh đại diện vượt quá 256 KiB.", ct);
        if (await users.SetAvatarAsync(id, bytes, ct) == 0)
            throw new BusinessException(403, "Tài khoản không còn hoạt động.");
        return await GetUserAsync(id, ct);
    }

    public async Task UpdateCompanyLogoAsync(int id, IFormFile image, CancellationToken ct)
    {
        var bytes = await ReadImageAsync(image, 1024 * 1024, "Logo công ty vượt quá 1 MiB.", ct);
        if (await users.SetCompanyLogoAsync(id, bytes, ct) == 0)
            throw new BusinessException(403, "Tài khoản nhà tuyển dụng không còn hoạt động.");
    }

    public async Task<(byte[] Bytes, string ContentType)> GetCompanyLogoAsync(int id, CancellationToken ct)
    {
        var bytes = await users.GetCompanyLogoAsync(id, ct);
        if (bytes == null || ImageType(bytes) is not { } type)
            throw new BusinessException(404, "Không tìm thấy logo công ty.");
        return (bytes, type);
    }

    private static async Task<byte[]> ReadImageAsync(IFormFile image, int maxBytes, string sizeError, CancellationToken ct)
    {
        if (image.Length > maxBytes) throw new BusinessException(413, sizeError);
        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (ImageType(bytes) == null)
            throw new BusinessException(400, "Vui lòng chọn ảnh JPEG hoặc PNG hợp lệ.");
        return bytes;
    }

    private static string? ImageType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 24 && bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 13, 10, 26, 10 })
            && bytes.Slice(12, 4).SequenceEqual("IHDR"u8) && bytes.Slice(bytes.Length - 8, 4).SequenceEqual("IEND"u8))
            return "image/png";
        if (bytes.Length > 10 && bytes.StartsWith(new byte[] { 0xff, 0xd8, 0xff }) && bytes.EndsWith(new byte[] { 0xff, 0xd9 }))
            return "image/jpeg";
        return null;
    }

    private static UserResponse ToResponse(User user)
    {
        string? avatar = user.AvatarImage is { } image && ImageType(image) is { } type
            ? $"data:{type};base64,{Convert.ToBase64String(image)}" : null;
        return new(user.Id, user.Email, user.FullName, user.Role, user.CompanyName, avatar, user.CompanyLogo != null,
            user.IsProfilePublic, user.ProfessionalTitle, user.ProfileSummary);
    }
}
