using System.ComponentModel.DataAnnotations;
using Recruitment.Api.Models;

namespace Recruitment.Api.DTOs;

public class AccountQuery : PageQuery
{
    [MaxLength(254)] public string? Keyword { get; set; }
    [EnumDataType(typeof(UserRole))] public UserRole? Role { get; set; }
}

public record AccountSummary(int Id, string FullName, string Email, UserRole Role, bool IsLocked, string? CompanyName);

public class EditAccountRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [MaxLength(150)] public string? CompanyName { get; set; }
}

public class AccountLockRequest
{
    [Required] public bool? IsLocked { get; set; }
}

public record DeletionImpact(int Jobs, int Applications);

public enum ReportPeriod { Day, Week, Month, All }

public class StatisticsQuery
{
    [EnumDataType(typeof(ReportPeriod))] public ReportPeriod Period { get; set; } = ReportPeriod.Day;
    public DateOnly? Date { get; set; }
}

public record AdminStatistics(ReportPeriod Period, DateOnly? StartDate, DateOnly? EndDateInclusive,
    int JobsPosted, int ApplicationsSubmitted, int Accepted, int Rejected, int ReviewsWithoutDate);
