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
        if (db.Database.GetDbConnection().Database != "RecruitmentWeb2Demo")
            throw new InvalidOperationException("Profile demo cần database riêng mang tên RecruitmentWeb2Demo.");

        await db.Database.MigrateAsync();
        if (!await db.Users.AnyAsync())
            await SeedJobsAsync(db);

        await AccountSetup.CreateIfMissingAsync(db, "client@viecmo.test", "ClientDemo2026!",
            "Ứng viên Demo", UserRole.Candidate);
        await AccountSetup.CreateIfMissingAsync(db, "admin@viecmo.test", "AdminDemo2026!",
            "Quản trị viên Demo", UserRole.Admin);
    }

    private static async Task SeedJobsAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var samples = new[]
        {
            ("Mây Studio", "Thiết kế sản phẩm số", "TP. Hồ Chí Minh", "Thiết kế giao diện web cùng nhóm sản phẩm. Bạn sẽ phác thảo luồng sử dụng, dựng prototype và điều chỉnh thiết kế dựa trên phản hồi của người dùng."),
            ("Sông Labs", "Lập trình viên Backend C#", "Hà Nội", "Xây dựng ASP.NET Core Web API cho hệ thống quản lý nội bộ. Công việc gồm thiết kế dữ liệu SQL Server, viết truy vấn và phối hợp với nhóm frontend."),
            ("Cam Creative", "Chuyên viên sáng tạo nội dung", "Đà Nẵng", "Lên ý tưởng, viết nội dung cho các kênh truyền thông và phối hợp với nhóm thiết kế để hoàn thiện ấn phẩm."),
            ("Lam Digital", "Thực tập sinh Frontend", "TP. Hồ Chí Minh", "Phát triển giao diện responsive bằng HTML, CSS và JavaScript. Nhóm sẽ hướng dẫn cách tích hợp API và kiểm tra trải nghiệm trên thiết bị di động."),
            ("Nếp Space", "Điều phối viên dự án", "Hà Nội", "Theo dõi tiến độ công việc, chuẩn bị tài liệu và kết nối các thành viên trong nhóm triển khai dự án."),
            ("Mộc Works", "Chuyên viên chăm sóc khách hàng", "Đà Nẵng", "Tiếp nhận yêu cầu, hướng dẫn khách hàng sử dụng sản phẩm và ghi nhận phản hồi để phối hợp với nhóm vận hành.")
        };

        var hasher = new PasswordHasher<User>();
        for (int index = 0; index < samples.Length; index++)
        {
            var (company, title, location, description) = samples[index];
            var employer = new User
            {
                Role = UserRole.Employer, FullName = company, CompanyName = company,
                Email = $"company-{index + 1}@demo.invalid"
            };
            employer.PasswordHash = hasher.HashPassword(employer, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            db.Jobs.Add(new Job
            {
                Employer = employer, Title = title, Description = description, Location = location,
                CreatedAt = now.AddDays(-index), Deadline = now.AddDays(30 + index), IsOpen = true
            });
        }
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            db.ChangeTracker.Clear();
            // Một tiến trình demo khác đã ghi bộ dữ liệu mẫu trước tiến trình này.
            // SaveChanges ghi cả bộ trong một transaction nên không có tin bị ghi dở.
        }
    }
}
