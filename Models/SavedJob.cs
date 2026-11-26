namespace Recruitment.Api.Models;

public class SavedJob
{
    public int CandidateId { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; }
}
