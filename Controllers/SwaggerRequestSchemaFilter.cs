using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recruitment.Api.Controllers;

// Mô tả các lựa chọn hợp lệ ở từng thao tác, kể cả khi DTO kết quả dùng chung enum.
public class SwaggerRequestSchemaFilter : ISchemaFilter, IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(JobsController) ||
            context.MethodInfo.Name is not (nameof(JobsController.CreateWithPdf) or nameof(JobsController.UpdateWithPdf))) return;
        var fields = operation.RequestBody.Content["multipart/form-data"].Schema.Properties;
        fields.Remove("JdUrl");
        fields.Remove("RemoveJd");
        if (context.MethodInfo.Name == nameof(JobsController.CreateWithPdf))
            fields.Remove("RowVersion");
        else
        {
            // Trên form HTTP, rowVersion là chuỗi Base64 dài 12 ký tự.
            fields["RowVersion"].Format = null;
            fields["RowVersion"].MinLength = fields["RowVersion"].MaxLength = 12;
            fields["RowVersion"].Description = "Có thể để trống hoặc dán rowVersion từ chi tiết tin.";
            fields["RowVersion"].Example = new OpenApiString("");
        }
        fields["Deadline"].Example = new OpenApiDateTime(DateTimeOffset.UtcNow.AddDays(30));
        fields["IsSalaryNegotiable"].Example = new OpenApiBoolean(false);
        fields["SalaryMin"].Example = new OpenApiInteger(12000000);
        fields["SalaryMax"].Example = new OpenApiInteger(18000000);
        fields["Pdf"].Description = "Bản mô tả công việc dạng PDF, tối đa 5 MiB.";
    }

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
            schema.Properties["isSalaryNegotiable"].Description = "true: thỏa thuận, bỏ qua hai số tiền. false: nhập cả salaryMin và salaryMax theo VNĐ/tháng. Bỏ qua cả ba trường để giữ thông tin hiện có.";
            schema.Example = new OpenApiObject
            {
                ["title"] = new OpenApiString("Thực tập sinh Backend"),
                ["description"] = new OpenApiString("Xây dựng API bằng ASP.NET Core."),
                ["location"] = new OpenApiString("Hà Nội"),
                ["isSalaryNegotiable"] = new OpenApiBoolean(false),
                ["salaryMin"] = new OpenApiInteger(12000000),
                ["salaryMax"] = new OpenApiInteger(18000000),
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
