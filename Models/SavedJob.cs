namespace Recruitment.Api.Models;

public class SavedJob
{
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public User Candidate { get; set; } = null!;
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; }
}
