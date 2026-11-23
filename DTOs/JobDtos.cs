using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.DTOs;

public class JobRequest
{
    [Required, MaxLength(150)] public string Title { get; set; } = "";
    [Required, MaxLength(4000)] public string Description { get; set; } = "";
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    [Required] public DateTimeOffset? Deadline { get; set; }
    public bool IsOpen { get; set; } = true;

    [Range(0, 1_000_000_000_000, ErrorMessage = "Lương tối thiểu không hợp lệ.")]
    public decimal? MinSalary { get; set; }

    [Range(0, 1_000_000_000_000, ErrorMessage = "Lương tối đa không hợp lệ.")]
    public decimal? MaxSalary { get; set; }
}

public enum JobSort { Newest, Deadline, Title, SalaryDesc }

public class UpdateJobRequest : JobRequest
{
    // SQL Server rowversion gồm 8 byte, được JSON biểu diễn bằng Base64.
    [MinLength(8), MaxLength(8)] public byte[]? RowVersion { get; set; }
}

public class JobQuery : PageQuery
{
    [MaxLength(150)] public string? Keyword { get; set; }
    [MaxLength(150)] public string? Location { get; set; }
    [EnumDataType(typeof(JobSort))] public JobSort Sort { get; set; } = JobSort.Newest;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
}

public record JobResponse(int Id, int EmployerId, string CompanyName, string Title, string Description,
    string Location, DateTimeOffset Deadline, bool IsOpen, DateTimeOffset CreatedAt, byte[] RowVersion, bool EmployerLocked,
    decimal? MinSalary = null, decimal? MaxSalary = null);

public record JobSummary(int OpenJobs, int Companies, int Locations, int NewThisWeek);
