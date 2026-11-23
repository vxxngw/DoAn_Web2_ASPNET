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
    public DateTimeOffset Deadline { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsOpen { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
