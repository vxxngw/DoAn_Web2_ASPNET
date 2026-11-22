using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recruitment.Api.Controllers;

// Mô tả các lựa chọn hợp lệ ở từng thao tác, kể cả khi DTO kết quả dùng chung enum.
public class SwaggerRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RegisterRequest))
        {
            schema.Properties["role"] = Choices(nameof(UserRole.Candidate), nameof(UserRole.Employer));
            schema.Example = new OpenApiObject
            {
                ["email"] = new OpenApiString("ungvien@example.test"),
                ["password"] = new OpenApiString("Demo-UngVien-2026"),
                ["fullName"] = new OpenApiString("Nguyễn An"),
                ["role"] = new OpenApiString(nameof(UserRole.Candidate))
            };
            schema.Properties["companyName"].Description = "Bắt buộc với Employer. Ứng viên bỏ qua trường này.";
        }
        if (context.Type == typeof(ReviewRequest))
            schema.Properties["status"] = Choices(nameof(ApplicationStatus.Accepted), nameof(ApplicationStatus.Rejected));

        if (typeof(JobRequest).IsAssignableFrom(context.Type))
        {
            schema.Properties["isOpen"].Default = new OpenApiBoolean(true);
            schema.Example = new OpenApiObject
            {
                ["title"] = new OpenApiString("Thực tập sinh Backend"),
                ["description"] = new OpenApiString("Xây dựng API bằng ASP.NET Core."),
                ["location"] = new OpenApiString("Hà Nội"),
                ["deadline"] = new OpenApiDateTime(DateTimeOffset.UtcNow.Date.AddDays(30)),
                ["isOpen"] = new OpenApiBoolean(true)
            };
        }
        if (context.Type == typeof(UpdateJobRequest))
        {
            var version = schema.Properties["rowVersion"];
            version.MinLength = version.MaxLength = 12; // 8 byte trong SQL Server tương ứng 12 ký tự Base64.
            version.Description = "Nếu gửi, dùng nguyên giá trị rowVersion từ GET chi tiết tin để phát hiện bản sửa cũ.";
        }
    }

    private static OpenApiSchema Choices(params string[] values) => new()
    {
        Type = "string", Enum = values.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList()
    };
}
