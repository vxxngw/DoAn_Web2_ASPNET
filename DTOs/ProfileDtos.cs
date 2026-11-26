using System.ComponentModel.DataAnnotations;
using Recruitment.Api.Models;

namespace Recruitment.Api.DTOs;

public class CvUploadRequest
{
    [Required] public IFormFile Cv { get; set; } = null!;
}

public record CvInfo(string FileName, int Size, DateTimeOffset UploadedAt);
public record ProfileResponse(CandidateProfileData Profile, CvInfo? Cv);
