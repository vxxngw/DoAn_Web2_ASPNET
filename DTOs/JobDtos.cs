using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.DTOs;

public class JobRequest : IValidatableObject
{
    [MaxLength(100)] public string? Industry { get; set; }
    [Required, MaxLength(150)] public string Title { get; set; } = "";
    [Required, MaxLength(4000)] public string Description { get; set; } = "";
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    [Required] public DateTimeOffset? Deadline { get; set; }
    public bool IsOpen { get; set; } = true;
    [MaxLength(2048)] public string? JdUrl { get; set; }
    public bool? IsSalaryNegotiable { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Lương tối thiểu phải lớn hơn 0.")] public int? SalaryMin { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Lương tối đa phải lớn hơn 0.")] public int? SalaryMax { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (JdUrl != null && (!Uri.TryCreate(JdUrl.Trim(), UriKind.Absolute, out var uri)
            || !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) || uri.Host.Length == 0 || uri.UserInfo.Length != 0))
            yield return new ValidationResult("Đường dẫn JD phải là URL HTTP hoặc HTTPS hợp lệ.", [nameof(JdUrl)]);
        if (IsSalaryNegotiable == false)
        {
            if (SalaryMin == null || SalaryMax == null)
                yield return new ValidationResult("Vui lòng nhập cả lương tối thiểu và tối đa.", [nameof(SalaryMin), nameof(SalaryMax)]);
            else if (SalaryMax < SalaryMin)
                yield return new ValidationResult("Lương tối đa phải bằng hoặc cao hơn lương tối thiểu.", [nameof(SalaryMax)]);
        }
        else if (SalaryMin != null || SalaryMax != null)
            yield return new ValidationResult("Chọn khoảng lương khi gửi số tiền.", [nameof(IsSalaryNegotiable)]);
    }
}

public enum JobSort { Newest, Deadline, Title, Featured }

public class UpdateJobRequest : JobRequest
{
    public bool RemoveJd { get; set; }
    // SQL Server rowversion gồm 8 byte, được JSON biểu diễn bằng Base64.
    [MinLength(8), MaxLength(8)] public byte[]? RowVersion { get; set; }
}

public class JobPdfRequest : UpdateJobRequest
{
    [Required] public IFormFile Pdf { get; set; } = null!;
}

public class JobQuery : PageQuery
{
    [MaxLength(100)] public string? Industry { get; set; }
    [Range(1, int.MaxValue)] public int? EmployerId { get; set; }
    [MaxLength(150)] public string? Keyword { get; set; }
    [MaxLength(150)] public string? Location { get; set; }
    [EnumDataType(typeof(JobSort))] public JobSort Sort { get; set; } = JobSort.Newest;
}

public record JobResponse(int Id, int EmployerId, string CompanyName, string Title, string Description,
    string Location, DateTimeOffset Deadline, bool IsOpen, DateTimeOffset CreatedAt, byte[] RowVersion, bool EmployerLocked,
    string? Industry = null, bool IsSaved = false, bool? IsSalaryNegotiable = null, int? SalaryMin = null, int? SalaryMax = null,
    bool HasCompanyLogo = false, string? JdUrl = null, bool HasJdPdf = false, bool Archived = false);

public record JobSummary(int OpenJobs, int Companies, int Locations, int NewThisWeek);

public record TopCompany(int EmployerId, string CompanyName, int OpenJobs, bool HasCompanyLogo);

public record IndustrySummary(string Industry, int OpenJobs);

public record SavedJobItem(int Id, string Title, string CompanyName, string Location, DateTimeOffset Deadline,
    bool IsOpen, bool EmployerLocked, DateTimeOffset SavedAt, bool? IsSalaryNegotiable, int? SalaryMin, int? SalaryMax,
    int EmployerId, bool HasCompanyLogo);
