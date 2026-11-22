using System.ComponentModel.DataAnnotations;
using Recruitment.Api.Models;

namespace Recruitment.Api.DTOs;

public class AccountQuery : PageQuery
{
    [MaxLength(254)] public string? Keyword { get; set; }
}

public record AccountSummary(int Id, string FullName, string Email, UserRole Role, bool IsLocked);

public class AccountLockRequest
{
    [Required] public bool? IsLocked { get; set; }
}
