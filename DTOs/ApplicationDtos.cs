using System.ComponentModel.DataAnnotations;
using Recruitment.Api.Models;

namespace Recruitment.Api.DTOs;

public class ApplyRequest
{
    [Required] public IFormFile Cv { get; set; } = null!;
    [Required, MaxLength(1500)] public string Message { get; set; } = "";
}

public class ApplicationQuery : PageQuery
{
    [MaxLength(150)] public string? Keyword { get; set; }
    [Range(1, int.MaxValue)] public int? JobId { get; set; }
    [EnumDataType(typeof(ApplicationStatus))] public ApplicationStatus? Status { get; set; }
}

public class ReviewRequest
{
    [Required] public ApplicationStatus? Status { get; set; }
}

public record ApplicationResponse(int Id, int JobId, string JobTitle, int CandidateId,
    string CandidateName, string CandidateEmail, string Message, ApplicationStatus Status, DateTimeOffset SubmittedAt);
