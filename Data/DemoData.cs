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
        if (await db.Jobs.CountAsync() < 500)
            await SeedLargeDatasetAsync(db);

        await AccountSetup.CreateIfMissingAsync(db, "client@viecmo.test", "ClientDemo2026!",
            "Ứng viên Demo", UserRole.Candidate);
        await AccountSetup.CreateIfMissingAsync(db, "admin@viecmo.test", "AdminDemo2026!",
            "Quản trị viên Demo", UserRole.Admin);
    }

    public static async Task SeedLargeDatasetAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher<User>();
        var dummyUser = new User();
        string sharedPasswordHash = hasher.HashPassword(dummyUser, "Demo123456!");

        var firstNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Đoàn", "Lâm", "Trịnh" };
        var middleNames = new[] { "Văn", "Thị", "Đức", "Minh", "Hải", "Tuấn", "Thanh", "Quốc", "Hồng", "Gia", "Bảo", "Ngọc", "Thu", "Khánh", "Phương", "Hoàng" };
        var lastNames = new[] { "Anh", "Dũng", "Huy", "Khoa", "Long", "Nam", "Phúc", "Quân", "Sơn", "Thịnh", "Tùng", "Cường", "Trang", "Linh", "Hà", "Mai", "Hương", "Lan", "Ngọc", "Yến", "Thảo", "Vy", "Huyền", "Trâm" };

        var companyPrefixes = new[] { "Công ty CP", "Tập đoàn", "Công ty TNHH", "Công nghệ", "Giải pháp Phần mềm", "Studio" };
        var companyBases = new[] {
            "FPT", "VNG", "Viettel", "VNPT", "MISA", "Tiki", "Shopee", "MoMo", "Zalo", "VinAI", "Sendo", "One Mount", "KMS Technology",
            "NashTech", "TMA Solutions", "Axon", "DEK Technologies", "Rikkeisoft", "Sun Asterisk", "VNPAY", "VPBank Digital", "Techcombank Software",
            "MB Ageas", "TPBank Innovation", "ACB Digital", "Funtap", "VCCorp", "NextTech", "SmartOSC", "Giao Hàng Tiết Kiệm", "Viettel Post",
            "Be Group", "Grab Vietnam", "Fossil Vietnam", "NAB Innovation", "Base Enterprise", "TopCV", "Navigos Group", "CMC Global",
            "HPT Vietnam", "KiotViet", "Haravan", "Sapo", "Bizfly", "Appota", "Amanotes", "Sky Mavis", "Kyber Network", "SotaTek", "Luvina"
        };

        var jobRoles = new[] {
            ("Lập trình viên Backend .NET", "Phát triển và bảo trì hệ thống Web API bằng C# và .NET Core, thiết kế cơ sở dữ liệu SQL Server và tối ưu hóa hiệu năng microservices."),
            ("Lập trình viên Backend Java / Spring Boot", "Xây dựng các dịch vụ backend hiệu năng cao sử dụng Spring Boot, Kafka và PostgreSQL."),
            ("Lập trình viên Fullstack (.NET & React)", "Phát triển toàn diện từ giao diện người dùng ReactJS đến kiến trúc backend ASP.NET Core."),
            ("Lập trình viên Frontend React / Next.js", "Xây dựng giao diện web responsive, tối ưu trải nghiệm người dùng bằng React, Redux và TailwindCSS."),
            ("Lập trình viên Frontend Vue.js / Nuxt", "Phát triển ứng dụng web hiện đại với Vue 3, Pinia và TypeScript."),
            ("Lập trình viên Mobile Flutter", "Xây dựng ứng dụng di động đa nền tảng iOS & Android với Flutter và Dart."),
            ("Lập trình viên Mobile React Native", "Phát triển ứng dụng mobile tương tác cao với React Native và Redux."),
            ("Lập trình viên iOS (Swift)", "Thiết kế và phát triển ứng dụng iOS native sử dụng Swift và SwiftUI."),
            ("Lập trình viên Android (Kotlin)", "Phát triển ứng dụng Android native chuẩn Jetpack Compose và Clean Architecture."),
            ("Kỹ sư Dữ liệu (Data Engineer)", "Thiết kế pipeline dữ liệu ETL với Python, Spark, Airflow và kho dữ liệu Snowflake/BigQuery."),
            ("Chuyên viên Phân tích Dữ liệu (Data Analyst)", "Khai thác dữ liệu, xây dựng dashboard trực quan hóa bằng PowerBI, Tableau và SQL."),
            ("Kỹ sư Học máy & AI (AI/ML Engineer)", "Nghiên cứu và triển khai mô hình học sâu, xử lý ngôn ngữ tự nhiên (NLP) và Computer Vision."),
            ("Kỹ sư DevOps / Cloud (AWS / Azure)", "Thiết lập hệ thống CI/CD tự động, quản lý hạ tầng đám mây với Terraform, Docker và Kubernetes."),
            ("Kỹ sư Kiểm thử phần mềm (QA/QC Manual)", "Lập kế hoạch kiểm thử, viết test case và kiểm tra chất lượng phần mềm trước khi phát hành."),
            ("Kỹ sư Kiểm thử tự động (Automation Tester)", "Xây dựng framework kiểm thử tự động với Selenium, Playwright hoặc Cypress."),
            ("Quản lý Dự án CNTT (IT Project Manager)", "Quản lý tiến độ, phạm vi và ngân sách dự án phần mềm; điều phối các nhóm kỹ thuật."),
            ("Scrum Master / Agile Coach", "Điều phối quy trình Agile/Scrum, gỡ bỏ trở ngại và nâng cao năng suất nhóm phát triển."),
            ("Chuyên viên Phân tích Nghiệp vụ (Business Analyst)", "Thu thập, phân tích yêu cầu từ khách hàng và chuyển hóa thành tài liệu đặc tả kỹ thuật BRD/SRS."),
            ("Chuyên viên Thiết kế UI/UX", "Nghiên cứu hành vi người dùng, phác thảo wireframe và thiết kế giao diện Figma chuyên nghiệp."),
            ("Kỹ sư An toàn Thông tin (Cyber Security)", "Đánh giá lỗ hổng bảo mật hệ thống (Penetration Testing), giám sát an ninh SOC."),
            ("Kỹ sư Hệ thống & Mạng (System Engineer)", "Quản trị hệ thống máy chủ Linux/Windows, cấu hình mạng và chính sách sao lưu dự phòng."),
            ("Chuyên viên Marketing số (Digital Marketing)", "Lên chiến lược và tối ưu các chiến dịch quảng cáo đa kênh Google Ads, Facebook Ads và SEO."),
            ("Chuyên viên Quản trị Nội dung (Content Creator)", "Sáng tạo nội dung truyền thông số, kịch bản video và bài viết chuyên ngành công nghệ."),
            ("Kỹ sư Lập trình Nhúng (Embedded Engineer)", "Lập trình C/C++ cho các vi điều khiển STM32, ESP32 và hệ điều hành thời gian thực RTOS."),
            ("Lập trình viên Python / Django", "Phát triển web app và API bằng Python, Django REST framework và Celery."),
            ("Lập trình viên Golang", "Xây dựng hệ thống phân tán microservices tốc độ cao với Golang và gRPC."),
            ("Kỹ sư Giải pháp Đám mây (Cloud Solution Architect)", "Tư vấn và thiết kế kiến trúc hệ thống đám mây quy mô lớn."),
            ("Chuyên viên Tuyển dụng IT (IT Recruiter)", "Tìm kiếm, kết nối và phỏng vấn các tài năng công nghệ thông tin cho doanh nghiệp."),
            ("Chuyên viên Hỗ trợ Kỹ thuật (IT Helpdesk)", "Tiếp nhận và xử lý các sự cố phần cứng, phần mềm mạng cho người dùng nội bộ."),
            ("Chuyên viên Triển khai ERP", "Tư vấn, cấu hình và triển khai hệ thống hoạch định nguồn lực doanh nghiệp SAP/Odoo.")
        };

        var cities = new[] { "TP. Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng", "Bình Dương", "Đồng Nai", "Huế" };

        var salaryBrackets = new (decimal min, decimal max)[] {
            (8_000_000m, 12_000_000m),
            (10_000_000m, 15_000_000m),
            (12_000_000m, 18_000_000m),
            (15_000_000m, 22_000_000m),
            (18_000_000m, 26_000_000m),
            (20_000_000m, 30_000_000m),
            (25_000_000m, 35_000_000m),
            (30_000_000m, 45_000_000m),
            (35_000_000m, 55_000_000m),
            (40_000_000m, 70_000_000m)
        };

        // 1. Tạo 500 Ứng viên
        int existingCandidates = await db.Users.CountAsync(u => u.Role == UserRole.Candidate && u.Email.StartsWith("candidate."));
        int candidatesToSeed = Math.Max(0, 500 - existingCandidates);
        var candidates = new List<User>(candidatesToSeed);
        for (int i = 1; i <= candidatesToSeed; i++)
        {
            int idx = existingCandidates + i;
            string fn = firstNames[(idx * 7) % firstNames.Length];
            string mn = middleNames[(idx * 11) % middleNames.Length];
            string ln = lastNames[(idx * 13) % lastNames.Length];
            candidates.Add(new User
            {
                FullName = $"{fn} {mn} {ln}",
                Email = $"candidate.{idx}@viecmo.test",
                Role = UserRole.Candidate,
                PasswordHash = sharedPasswordHash,
                IsLocked = (idx % 50 == 0)
            });
        }
        if (candidates.Count > 0)
        {
            await db.Users.AddRangeAsync(candidates);
            await db.SaveChangesAsync();
        }

        // 2. Tạo 500 Nhà tuyển dụng và 500 Tin tuyển dụng
        int existingJobs = await db.Jobs.CountAsync();
        int jobsToSeed = Math.Max(0, 500 - existingJobs);
        var employers = new List<User>(jobsToSeed);
        var jobs = new List<Job>(jobsToSeed);

        for (int i = 1; i <= jobsToSeed; i++)
        {
            int idx = existingJobs + i;
            string pre = companyPrefixes[(idx * 3) % companyPrefixes.Length];
            string baseName = companyBases[(idx * 5) % companyBases.Length];
            string company = $"{pre} {baseName} {idx}";
            string email = $"employer.{idx}@viecmo.test";
            var employer = new User
            {
                FullName = company,
                CompanyName = company,
                Email = email,
                Role = UserRole.Employer,
                PasswordHash = sharedPasswordHash,
                IsLocked = false
            };
            employers.Add(employer);

            var (roleTitle, roleDesc) = jobRoles[(idx * 7) % jobRoles.Length];
            string city = cities[(idx * 2) % cities.Length];
            var (minSal, maxSal) = salaryBrackets[(idx * 4) % salaryBrackets.Length];

            jobs.Add(new Job
            {
                Employer = employer,
                Title = $"{roleTitle} #{idx}",
                Description = $"{roleDesc}\n\nYêu cầu ứng viên:\n- Có từ 1-3 năm kinh nghiệm trong lĩnh vực liên quan.\n- Tinh thần trách nhiệm cao, chủ động trong công việc.\n- Khả năng làm việc nhóm tốt và tư duy giải quyết vấn đề.",
                Location = city,
                MinSalary = minSal,
                MaxSalary = maxSal,
                CreatedAt = now.AddDays(-(idx % 45)),
                Deadline = now.AddDays(15 + (idx % 60)),
                IsOpen = (idx % 25 != 0)
            });
        }

        if (jobs.Count > 0)
        {
            await db.Jobs.AddRangeAsync(jobs);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedJobsAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var samples = new (string company, string title, string location, string description, decimal? min, decimal? max)[]
        {
            ("Mây Studio", "Thiết kế sản phẩm số", "TP. Hồ Chí Minh", "Thiết kế giao diện web cùng nhóm sản phẩm. Bạn sẽ phác thảo luồng sử dụng, dựng prototype và điều chỉnh thiết kế dựa trên phản hồi của người dùng.", 18_000_000m, 25_000_000m),
            ("Sông Labs", "Lập trình viên Backend C#", "Hà Nội", "Xây dựng ASP.NET Core Web API cho hệ thống quản lý nội bộ. Công việc gồm thiết kế dữ liệu SQL Server, viết truy vấn và phối hợp với nhóm frontend.", 22_000_000m, 35_000_000m),
            ("Cam Creative", "Chuyên viên sáng tạo nội dung", "Đà Nẵng", "Lên ý tưởng, viết nội dung cho các kênh truyền thông và phối hợp với nhóm thiết kế để hoàn thiện ấn phẩm.", 12_000_000m, 16_000_000m),
            ("Lam Digital", "Thực tập sinh Frontend", "TP. Hồ Chí Minh", "Phát triển giao diện responsive bằng HTML, CSS và JavaScript. Nhóm sẽ hướng dẫn cách tích hợp API và kiểm tra trải nghiệm trên thiết bị di động.", 6_000_000m, 9_000_000m),
            ("Nếp Space", "Điều phối viên dự án", "Hà Nội", "Theo dõi tiến độ công việc, chuẩn bị tài liệu và kết nối các thành viên trong nhóm triển khai dự án.", 15_000_000m, 20_000_000m),
            ("Mộc Works", "Chuyên viên chăm sóc khách hàng", "Đà Nẵng", "Tiếp nhận yêu cầu, hướng dẫn khách hàng sử dụng sản phẩm và ghi nhận phản hồi để phối hợp với nhóm vận hành.", 10_000_000m, 14_000_000m)
        };

        var hasher = new PasswordHasher<User>();
        for (int index = 0; index < samples.Length; index++)
        {
            var (company, title, location, description, min, max) = samples[index];
            var employer = new User
            {
                Role = UserRole.Employer, FullName = company, CompanyName = company,
                Email = $"company-{index + 1}@demo.invalid"
            };
            employer.PasswordHash = hasher.HashPassword(employer, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            db.Jobs.Add(new Job
            {
                Employer = employer, Title = title, Description = description, Location = location,
                MinSalary = min, MaxSalary = max,
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
