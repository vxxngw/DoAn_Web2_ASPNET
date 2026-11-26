using System.ComponentModel.DataAnnotations;
using Recruitment.Api.Models;

namespace Recruitment.Api.DTOs;

public class RegisterRequest : IValidatableObject
{
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required] public UserRole? Role { get; set; }
    [MaxLength(150)] public string? CompanyName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Role == UserRole.Employer && string.IsNullOrWhiteSpace(CompanyName))
            yield return new ValidationResult("Nhà tuyển dụng phải có tên công ty.", [nameof(CompanyName)]);
        if (Role == UserRole.Candidate && !string.IsNullOrWhiteSpace(CompanyName))
            yield return new ValidationResult("Chỉ nhà tuyển dụng có tên công ty.", [nameof(CompanyName)]);
    }
}

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, MaxLength(128)] public string Password { get; set; } = "";
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại."), MaxLength(128)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có từ 8 đến 128 ký tự.")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới."), MaxLength(128)]
    [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu mới không khớp.")]
    public string ConfirmNewPassword { get; set; } = "";
}

public class PersonalAccountRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [MaxLength(150)] public string? CompanyName { get; set; }
}

public class ImageUploadRequest
{
    [Required] public IFormFile Image { get; set; } = null!;
}

public class CandidateProfileRequest
{
    [Required] public bool? IsProfilePublic { get; set; }
    [MaxLength(150)] public string? ProfessionalTitle { get; set; }
    [MaxLength(1500)] public string? ProfileSummary { get; set; }
}

public record UserResponse(int Id, string Email, string FullName, UserRole Role, string? CompanyName, string? AvatarDataUrl, bool HasCompanyLogo,
    bool IsProfilePublic, string? ProfessionalTitle, string? ProfileSummary);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
