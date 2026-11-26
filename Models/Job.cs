using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.Models;

public class Job
{
    public int Id { get; set; }
    public int EmployerId { get; set; }
    public User Employer { get; set; } = null!;
    [MaxLength(150)] public string Title { get; set; } = "";
    [MaxLength(4000)] public string Description { get; set; } = "";
    [MaxLength(150)] public string Location { get; set; } = "";
    [MaxLength(100)] public string? Industry { get; set; }
    public bool? IsSalaryNegotiable { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsOpen { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    [MaxLength(2048)] public string? JdUrl { get; set; }
    public byte[]? JdPdf { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
