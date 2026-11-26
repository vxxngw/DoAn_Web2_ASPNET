using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.Models;

// Nội dung hồ sơ ứng viên. API nhận đúng lớp này, entity kế thừa để chỉ khai báo trường một lần.
// Chức danh, giới thiệu và chế độ công khai nằm trên User vì danh bạ ứng viên của nhà tuyển dụng dùng chúng.
public class CandidateProfileData
{
    [Range(0, 50)] public int? YearsOfExperience { get; set; }
    [MaxLength(50)] public string? CurrentLevel { get; set; }
    [MaxLength(50)] public string? HighestDegree { get; set; }
    [MaxLength(20), RegularExpression(@"^\+?[0-9 .()-]{8,20}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; set; }
    [MaxLength(250)] public string? Address { get; set; }
    [MaxLength(100)] public string? DesiredLocation { get; set; }
    [Range(0, int.MaxValue)] public int? DesiredSalary { get; set; }
    [Required, MaxLength(30)] public List<WorkExperience> Experiences { get; set; } = [];
    [Required, MaxLength(20)] public List<Education> Educations { get; set; } = [];
    [Required, MaxLength(50)] public List<Skill> Skills { get; set; } = [];
    [Required, MaxLength(10)] public List<LanguageSkill> Languages { get; set; } = [];
}

public class CandidateProfile : CandidateProfileData
{
    [Key] public int UserId { get; set; }
}

public class CandidateCv
{
    [Key] public int UserId { get; set; }
    [MaxLength(255)] public string FileName { get; set; } = "";
    [MaxLength(100)] public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public DateTimeOffset UploadedAt { get; set; }
}

public abstract class Period : IValidatableObject
{
    [Required] public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (To < From)
            yield return new ValidationResult("Thời gian kết thúc phải sau thời gian bắt đầu.", [nameof(To)]);
    }
}

public class WorkExperience : Period
{
    [Required, MaxLength(100)] public string Title { get; set; } = "";
    [Required, MaxLength(150)] public string Company { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
}

public class Education : Period
{
    [Required, MaxLength(150)] public string School { get; set; } = "";
    [MaxLength(150)] public string? Major { get; set; }
}

public class Skill
{
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    [Range(0, 5)] public int Level { get; set; }
}

public class LanguageSkill
{
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    [Required, MaxLength(20)] public string Level { get; set; } = "";
}
