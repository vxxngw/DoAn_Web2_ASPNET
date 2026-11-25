using System.Text;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.Models;

namespace Recruitment.Api.Services;

public interface IApplicationExportService
{
    Task<(byte[] CsvBytes, string FileName)> ExportApplicationsCsvAsync(int jobId, int? employerId, bool isAdmin, CancellationToken ct);
}

public class ApplicationExportService(AppDbContext db) : IApplicationExportService
{
    public async Task<(byte[] CsvBytes, string FileName)> ExportApplicationsCsvAsync(int jobId, int? employerId, bool isAdmin, CancellationToken ct)
    {
        var job = await db.Jobs
            .AsNoTracking()
            .Where(j => j.Id == jobId)
            .Select(j => new { j.Id, j.EmployerId, j.Title })
            .SingleOrDefaultAsync(ct);

        if (job == null)
            throw new BusinessException(404, "Không tìm thấy công việc.");

        if (!isAdmin && job.EmployerId != employerId)
            throw new BusinessException(403, "Bạn không có quyền xuất danh sách ứng viên của tin này.");

        var applications = await db.Applications
            .AsNoTracking()
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new
            {
                a.Id,
                CandidateName = a.Candidate.FullName,
                CandidateEmail = a.Candidate.Email,
                JobTitle = a.Job.Title,
                SubmittedAt = a.SubmittedAt,
                Status = a.Status,
                Message = a.Message
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("STT,Mã hồ sơ,Họ và tên ứng viên,Email,Vị trí ứng tuyển,Thời gian nộp,Trạng thái,Lời nhắn");

        int index = 1;
        foreach (var app in applications)
        {
            string statusText = app.Status switch
            {
                ApplicationStatus.Submitted => "Chờ duyệt",
                ApplicationStatus.Accepted => "Đã duyệt nhận",
                ApplicationStatus.Rejected => "Từ chối",
                ApplicationStatus.Withdrawn => "Đã rút hồ sơ",
                _ => app.Status.ToString()
            };

            var vnTime = app.SubmittedAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm");

            sb.AppendLine(string.Join(",",
                index++,
                app.Id,
                Escape(app.CandidateName),
                Escape(app.CandidateEmail),
                Escape(app.JobTitle),
                Escape(vnTime),
                Escape(statusText),
                Escape(app.Message)));
        }

        // Thêm UTF-8 BOM để Excel hiển thị tiếng Việt có dấu chuẩn xác
        byte[] bom = [0xEF, 0xBB, 0xBF];
        byte[] textBytes = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] result = new byte[bom.Length + textBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(textBytes, 0, result, bom.Length, textBytes.Length);

        string safeTitle = string.Concat(job.Title.Split(Path.GetInvalidFileNameChars())).Replace(" ", "-");
        if (safeTitle.Length > 30) safeTitle = safeTitle[..30];
        string fileName = $"ung-vien-{jobId}-{safeTitle}.csv";

        return (result, fileName);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ") + "\"";
    }
}
