using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IProfileService
{
    Task<ProfileResponse> GetAsync(int userId, CancellationToken ct);
    Task SaveAsync(int userId, CandidateProfileData data, CancellationToken ct);
    Task UploadCvAsync(int userId, IFormFile file, CancellationToken ct);
    Task<CandidateCv> GetCvAsync(int userId, CancellationToken ct);
}

public class ProfileService(IProfileRepository profiles) : IProfileService
{
    private const int MaxCvBytes = 5 * 1024 * 1024;

    public async Task<ProfileResponse> GetAsync(int userId, CancellationToken ct) =>
        new(await profiles.FindAsync(userId, ct) ?? new CandidateProfile(), await profiles.FindCvInfoAsync(userId, ct));

    public async Task SaveAsync(int userId, CandidateProfileData data, CancellationToken ct)
    {
        if (!await profiles.TrySaveAsync(userId, data, ct))
            throw new BusinessException(409, "Hồ sơ vừa được cập nhật ở nơi khác. Vui lòng thử lại.");
    }

    public async Task UploadCvAsync(int userId, IFormFile file, CancellationToken ct)
    {
        if (file.Length > MaxCvBytes)
            throw new BusinessException(413, "CV vượt quá 5 MiB.");
        string name = Path.GetFileName(file.FileName);
        if (name.Length > 255)
            throw new BusinessException(400, "Tên tệp CV dài quá 255 ký tự.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        byte[] content = buffer.ToArray();
        string type = DocumentType(Path.GetExtension(name).ToLowerInvariant(), content)
            ?? throw new BusinessException(400, "Vui lòng chọn CV định dạng .pdf, .doc hoặc .docx hợp lệ.");

        var cv = new CandidateCv { UserId = userId, FileName = name, ContentType = type, Content = content, UploadedAt = DateTimeOffset.UtcNow };
        if (!await profiles.TrySaveCvAsync(cv, ct))
            throw new BusinessException(409, "CV vừa được cập nhật ở nơi khác. Vui lòng thử lại.");
    }

    public async Task<CandidateCv> GetCvAsync(int userId, CancellationToken ct) =>
        await profiles.FindCvAsync(userId, ct) ?? throw new BusinessException(404, "Bạn chưa tải CV đính kèm.");

    // Đuôi tệp phải khớp chữ ký nội dung. Content-Type do máy chủ tự xác định, không lấy từ trình duyệt.
    private static string? DocumentType(string extension, ReadOnlySpan<byte> content) => extension switch
    {
        ".pdf" when content.StartsWith("%PDF-"u8) => "application/pdf",
        ".docx" when content.StartsWith("PK\x03\x04"u8) => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" when content.StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }) => "application/msword",
        _ => null
    };
}
