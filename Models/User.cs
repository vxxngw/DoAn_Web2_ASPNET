using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.Models;

public enum UserRole { Candidate, Employer, Admin }

public class User
{
    public int Id { get; set; }
    [MaxLength(254)] public string Email { get; set; } = "";
    [MaxLength(100)] public string FullName { get; set; } = "";
    [MaxLength(150)] public string? CompanyName { get; set; }
    public UserRole Role { get; set; }
    public bool IsLocked { get; set; }
    [MaxLength(512)] public string PasswordHash { get; set; } = "";
}
