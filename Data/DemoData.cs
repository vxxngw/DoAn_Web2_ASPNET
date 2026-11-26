using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

// Chỉ profile demo gọi lớp này, trên database dành riêng cho buổi thực hành.
public static class DemoData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        if (!await db.Users.AnyAsync())
            await SeedJobsAsync(db);

        // Admin demo đánh dấu lần khởi tạo đầu tiên. Giữ quyết định xóa hoặc đổi email client ở các lần chạy sau.
        if (!await db.Users.AnyAsync(u => u.Email == "admin@viecmo.test" && u.Role == UserRole.Admin))
        {
            await AccountSetup.CreateIfMissingAsync(db, "client@viecmo.test", "ClientDemo2026!",
                "Ứng viên Demo", UserRole.Candidate);
            await AccountSetup.CreateIfMissingAsync(db, "employer@viecmo.test", "EmployerDemo2026!",
                "Nhà tuyển dụng Mây Studio", UserRole.Employer, "Mây Studio");
            await AccountSetup.CreateIfMissingAsync(db, "admin@viecmo.test", "AdminDemo2026!",
                "Quản trị viên Demo", UserRole.Admin);
        }
    }

    private static async Task SeedJobsAsync(AppDbContext db)
    {
        var jobs = CreateSampleJobs(DateTimeOffset.UtcNow);
        var hasher = new PasswordHasher<User>();
        foreach (var job in jobs)
            job.Employer.PasswordHash = hasher.HashPassword(job.Employer, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        db.Jobs.AddRange(jobs);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            db.ChangeTracker.Clear();
            // Một tiến trình demo khác đã ghi bộ dữ liệu mẫu trước tiến trình này.
        }
    }

    internal static Job[] CreateSampleJobs(DateTimeOffset now)
    {
        var samples = new[]
        {
            ("Mây Studio", "Thiết kế sản phẩm số", "TP. Hồ Chí Minh", "Thiết kế giao diện web cùng nhóm sản phẩm. Bạn sẽ phác thảo luồng sử dụng, dựng prototype và điều chỉnh thiết kế dựa trên phản hồi của người dùng."),
            ("Sông Labs", "Lập trình viên Backend C#", "Hà Nội", "Xây dựng ASP.NET Core Web API cho hệ thống quản lý nội bộ. Công việc gồm thiết kế dữ liệu SQL Server, viết truy vấn và phối hợp với nhóm frontend."),
            ("Cam Creative", "Chuyên viên sáng tạo nội dung", "Đà Nẵng", "Lên ý tưởng, viết nội dung cho các kênh truyền thông và phối hợp với nhóm thiết kế để hoàn thiện ấn phẩm."),
            ("Lam Digital", "Thực tập sinh Frontend", "TP. Hồ Chí Minh", "Phát triển giao diện responsive bằng HTML, CSS và JavaScript. Nhóm sẽ hướng dẫn cách tích hợp API và kiểm tra trải nghiệm trên thiết bị di động."),
            ("Nếp Space", "Điều phối viên dự án", "Hà Nội", "Theo dõi tiến độ công việc, chuẩn bị tài liệu và kết nối các thành viên trong nhóm triển khai dự án."),
            ("Mộc Works", "Chuyên viên chăm sóc khách hàng", "Đà Nẵng", "Tiếp nhận yêu cầu, hướng dẫn khách hàng sử dụng sản phẩm và ghi nhận phản hồi để phối hợp với nhóm vận hành.")
        };

        return samples.Select((sample, index) =>
        {
            var (company, title, location, description) = sample;
            var employer = new User
            {
                Role = UserRole.Employer, FullName = company, CompanyName = company,
                Email = $"company-{index + 1}@demo.invalid"
            };
            var job = new Job
            {
                Employer = employer, Title = title, Industry = IndustryForSample(title), Description = description, Location = location,
                CreatedAt = now.AddDays(-index), Deadline = now.AddDays(30 + index), IsOpen = true
            };
            DemoListingSamples.SetSalary(job);
            return job;
        }).ToArray();
    }

    internal static string? IndustryForSample(string title) => title switch
    {
        "Lập trình viên Backend C#" or "Thực tập sinh Frontend" or "Kiểm thử viên phần mềm" or "Kỹ sư dữ liệu" or "Chuyên viên hỗ trợ kỹ thuật" or "Chuyên viên phân tích nghiệp vụ" => "Công nghệ thông tin",
        "Thiết kế UI/UX" or "Thiết kế sản phẩm số" => "Thiết kế",
        "Nhân viên kinh doanh" or "Chuyên viên thương mại điện tử" or "Nhân viên phát triển thị trường" => "Kinh doanh & thương mại",
        "Trợ lý thu mua" or "Nhân viên kho vận" => "Logistics & thu mua",
        "Chuyên viên marketing" or "Chuyên viên sáng tạo nội dung" or "Nhân viên tổ chức sự kiện" => "Marketing & truyền thông",
        "Kế toán bán hàng" => "Kế toán",
        "Điều phối viên dự án" or "Trợ lý vận hành" => "Quản lý & vận hành",
        "Chuyên viên chăm sóc khách hàng" => "Chăm sóc khách hàng",
        "Nhân viên hành chính" or "Chuyên viên tuyển dụng" or "Chuyên viên đào tạo" => "Hành chính & nhân sự",
        _ => null
    };
}
