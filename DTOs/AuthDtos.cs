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

public record UserResponse(int Id, string Email, string FullName, UserRole Role, string? CompanyName);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
