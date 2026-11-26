using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

internal static class DemoListingSamples
{
    // Các mức minh họa tính theo triệu đồng/tháng. Những vị trí còn lại dùng Thỏa thuận.
    internal static void SetSalary(Job job)
    {
        (int Min, int Max)? range = job.Title switch
        {
            "Lập trình viên Backend C#" => (18, 30),
            "Thực tập sinh Frontend" => (4, 7),
            "Kỹ sư dữ liệu" => (22, 35),
            "Kiểm thử viên phần mềm" or "Thiết kế UI/UX" => (12, 20),
            "Thiết kế sản phẩm số" => (16, 25),
            "Chuyên viên marketing" or "Chuyên viên thương mại điện tử" => (12, 18),
            "Nhân viên kho vận" or "Nhân viên hành chính" => (8, 12),
            "Trợ lý thu mua" or "Trợ lý vận hành" => (9, 14),
            "Kế toán bán hàng" or "Chuyên viên chăm sóc khách hàng" => (10, 15),
            "Chuyên viên hỗ trợ kỹ thuật" or "Chuyên viên tuyển dụng" or "Chuyên viên đào tạo" => (12, 17),
            _ => null
        };
        job.IsSalaryNegotiable = range == null;
        job.SalaryMin = range?.Min * 1_000_000;
        job.SalaryMax = range?.Max * 1_000_000;
    }

    private static readonly (string Email, string Company, string File)[] Logos =
    [
        ("ntd013@seed500.test", "Công nghệ Bình Minh", "binh-minh-tech"),
        ("ntd001@seed500.test", "Công nghệ Mộc Lan", "moc-lan"),
        ("ntd004@seed500.test", "Công nghệ Sao Mai", "sao-mai"),
        ("ntd015@seed500.test", "Dịch vụ Bình Minh", "binh-minh-services")
    ];

    internal static byte[]? ReadLogo(string email, string company)
    {
        var file = Logos.FirstOrDefault(l => l.Email == email && l.Company == company).File;
        return file == null ? null : ReadPng(file);
    }

    private static byte[] ReadPng(string file)
    {
        using var stream = typeof(DemoListingSamples).Assembly.GetManifestResourceStream($"Recruitment.Api.Logos.{file}.png")
            ?? throw new InvalidOperationException($"Thiếu logo mẫu {file}.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    internal static async Task<(int Jobs, int Logos)> FillMissingAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var samples = DemoTestData.CreateJobs(DemoTestData.CreateEmployers(), now).Concat(DemoData.CreateSampleJobs(now));
        var original = samples.Select(j => (j.Employer.Email, j.Title, j.Description)).ToHashSet();
        var emails = original.Select(key => key.Email).Distinct().ToArray();
        await using var tx = await db.Database.BeginTransactionAsync();
        var rows = await db.Jobs.Where(j => j.IsSalaryNegotiable == null && j.SalaryMin == null && j.SalaryMax == null
                && j.Employer.Role == UserRole.Employer && emails.Contains(j.Employer.Email))
            .Select(j => new { Job = j, j.Employer.Email }).ToListAsync();
        foreach (var row in rows)
            if (original.Contains((row.Email, row.Job.Title, row.Job.Description)))
                SetSalary(row.Job);
        int jobs = await db.SaveChangesAsync();

        int logos = 0;
        foreach (var logo in Logos)
        {
            var bytes = ReadPng(logo.File);
            logos += await db.Users.Where(u => u.Email == logo.Email && u.CompanyName == logo.Company
                    && u.Role == UserRole.Employer && u.CompanyLogo == null)
                .ExecuteUpdateAsync(set => set.SetProperty(u => u.CompanyLogo, bytes));
        }
        await tx.CommitAsync();
        return (jobs, logos);
    }
}
