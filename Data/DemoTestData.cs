using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

internal static class DemoTestData
{
    private const string Domain = "@seed500.test";
    private const string DemoPassword = "DemoData500!";

    public static async Task<bool> SeedAsync(AppDbContext db, IPasswordHasher<User> hasher)
    {
        var employers = CreateEmployers();
        var candidates = CreateCandidates();
        var users = employers.Concat(candidates).ToArray();
        if (await AlreadySeededAsync(db, users))
            return false;

        foreach (var user in users)
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        foreach (var employer in employers)
            employer.CompanyLogo = DemoListingSamples.ReadLogo(employer.Email, employer.CompanyName!);

        var now = DateTimeOffset.UtcNow;
        var jobs = CreateJobs(employers, now);
        using var stream = typeof(DemoTestData).Assembly.GetManifestResourceStream("Recruitment.Api.DemoCv.pdf")
            ?? throw new InvalidOperationException("Thiếu CV PDF mẫu.");
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        db.Users.AddRange(users);
        db.Jobs.AddRange(jobs);
        db.Applications.AddRange(CreateApplications(candidates, jobs, now, buffer.ToArray()));
        try
        {
            // EF ghi cả ba bảng trong cùng một transaction.
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            db.ChangeTracker.Clear();
            if (await AlreadySeededAsync(db, users))
                return false;
            throw;
        }
    }

    private static async Task<bool> AlreadySeededAsync(AppDbContext db, User[] users)
    {
        var emails = users.Select(u => u.Email).ToArray();
        var existing = await db.Users.Where(u => emails.Contains(u.Email))
            .Select(u => new { u.Email, u.Role }).ToDictionaryAsync(u => u.Email, u => u.Role, StringComparer.OrdinalIgnoreCase);
        if (existing.Count == 0)
            return false;
        if (users.All(u => existing.TryGetValue(u.Email, out var role) && role == u.Role))
            return true;
        throw new InvalidOperationException("Email của bộ mẫu đã được dùng một phần hoặc sai vai trò. Dừng để giữ dữ liệu hiện có.");
    }

    internal static User[] CreateEmployers()
    {
        string[] sectors = ["Công nghệ", "Thương mại", "Dịch vụ"];
        string[] brands = ["Mộc Lan", "Sao Mai", "Sông Lam", "Trúc Việt", "Bình Minh",
            "Hải Vân", "Thiên Thanh", "Hướng Dương", "An Phú", "Cánh Buồm"];
        return Enumerable.Range(0, 30).Select(i =>
        {
            var company = $"{sectors[i % sectors.Length]} {brands[i / sectors.Length]}";
            return new User { Email = $"ntd{i + 1:D3}{Domain}", FullName = $"Nhân sự {company}",
                CompanyName = company, Role = UserRole.Employer };
        }).ToArray();
    }

    private static User[] CreateCandidates()
    {
        string[] surnames = ["Nguyễn Minh", "Trần Gia", "Lê Khánh", "Phạm Thanh", "Hoàng Bảo",
            "Vũ Thảo", "Đặng Ngọc", "Bùi Hải", "Đỗ Phương", "Lý Quỳnh"];
        string[] names = ["An", "Bình", "Chi", "Duy", "Giang", "Hà", "Linh", "Nam", "Tâm", "Vy"];
        return Enumerable.Range(0, 100).Select(i => new User
        {
            Email = $"uv{i + 1:D3}{Domain}", FullName = $"{surnames[i / names.Length]} {names[i % names.Length]}",
            Role = UserRole.Candidate
        }).ToArray();
    }

    internal static Job[] CreateJobs(User[] employers, DateTimeOffset now)
    {
        string[][] titles = [
            ["Lập trình viên Backend C#", "Thực tập sinh Frontend", "Kiểm thử viên phần mềm", "Kỹ sư dữ liệu",
             "Chuyên viên hỗ trợ kỹ thuật", "Thiết kế UI/UX", "Chuyên viên phân tích nghiệp vụ"],
            ["Nhân viên kinh doanh", "Trợ lý thu mua", "Chuyên viên marketing", "Nhân viên kho vận",
             "Chuyên viên thương mại điện tử", "Kế toán bán hàng", "Nhân viên phát triển thị trường"],
            ["Điều phối viên dự án", "Chuyên viên chăm sóc khách hàng", "Nhân viên hành chính", "Trợ lý vận hành",
             "Chuyên viên tuyển dụng", "Nhân viên tổ chức sự kiện", "Chuyên viên đào tạo"]
        ];
        string[] locations = ["Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Hải Phòng",
            "Cần Thơ", "Huế", "Quy Nhơn", "Nha Trang"];
        string[] activities = ["Phối hợp với nhóm sản phẩm, ghi nhận yêu cầu và kiểm tra kết quả trước khi bàn giao.",
            "Theo dõi hoạt động kinh doanh, tổng hợp số liệu và phối hợp với các đối tác.",
            "Tiếp nhận yêu cầu, sắp xếp tiến độ và hỗ trợ khách hàng trong quá trình sử dụng dịch vụ."];
        var jobs = new Job[200];
        for (int i = 0; i < jobs.Length; i++)
        {
            int sector = i % titles.Length;
            var employer = employers[i % employers.Length];
            string title = titles[sector][i / employers.Length];
            // Trong mỗi 20 tin có 15 tin đang tuyển, 3 tin đóng sớm và 2 tin đã hết hạn.
            int slot = i % 20;
            bool expired = slot >= 18;
            jobs[i] = new Job
            {
                Employer = employer, Title = title, Industry = DemoData.IndustryForSample(title), Location = locations[i % locations.Length],
                Description = $"{employer.CompanyName} tuyển {title}. {activities[sector]}",
                CreatedAt = now.AddDays(-1 - i % 32 - (expired ? 8 : 0)),
                Deadline = expired ? now.AddDays(-1 - i % 5) : now.AddDays(3 + i % 43),
                IsOpen = slot < 15 || expired
            };
            DemoListingSamples.SetSalary(jobs[i]);
        }
        return jobs;
    }

    private static List<JobApplication> CreateApplications(User[] candidates, Job[] jobs, DateTimeOffset now, byte[] pdf)
    {
        var applications = new List<JobApplication>();
        for (int i = 0; i < 170; i++)
        {
            var candidate = candidates[i % candidates.Length];
            // Phân đơn cho 120 tin để có cả tin chưa nhận đơn và tin có nhiều ứng viên.
            var job = jobs[i * 37 % 120];
            int slot = (i + 5 * (i / candidates.Length)) % 10;
            var status = slot < 5 ? ApplicationStatus.Submitted
                : slot < 8 ? ApplicationStatus.Accepted : ApplicationStatus.Rejected;
            var lastDay = job.Deadline < now ? job.Deadline : now;
            var submitted = job.CreatedAt.AddTicks((lastDay - job.CreatedAt).Ticks * (30 + i % 60) / 100);
            applications.Add(new JobApplication
            {
                Candidate = candidate, Job = job, Status = status, CvPdf = pdf, SubmittedAt = submitted,
                ReviewedAt = status == ApplicationStatus.Submitted ? null : submitted.AddTicks((now - submitted).Ticks / 2),
                Message = $"Tôi là {candidate.FullName}, mong được trao đổi về vị trí {job.Title}. Tôi gửi kèm CV để công ty tham khảo."
            });
        }
        return applications;
    }
}
