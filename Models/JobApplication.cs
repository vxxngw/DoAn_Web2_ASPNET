using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.Models;

public enum ApplicationStatus { Submitted, Accepted, Rejected }

public class JobApplication
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public int CandidateId { get; set; }
    public User Candidate { get; set; } = null!;
    [MaxLength(1500)] public string Message { get; set; } = "";
    public byte[] CvPdf { get; set; } = [];
    public ApplicationStatus Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
