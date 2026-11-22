using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Recruitment.Api.Controllers;

// Chạy trước model binding. File được giới hạn 5 MiB tại service, còn toàn bộ form có 6 MiB.
[AttributeUsage(AttributeTargets.Method)]
public sealed class CvUploadLimitAttribute : Attribute, IAsyncResourceFilter
{
    private const long MaxFormBytes = 6 * 1024 * 1024;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var http = context.HttpContext;
        long? length = http.Request.ContentLength;
        if (length is null && http.Request.HasFormContentType)
            length = await MeasureFormAsync(http.Request, http.RequestAborted);

        if (length > MaxFormBytes)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "Form nộp hồ sơ vượt quá 6 MiB. CV tối đa 5 MiB.",
                Instance = http.Request.Path
            })
            {
                StatusCode = StatusCodes.Status413PayloadTooLarge,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }

    // Khi thiếu Content-Length, chỉ đọc tối đa giới hạn + 1 byte. ASP.NET tự dọn
    // bộ đệm sau request. Tua lại để MVC đọc form hợp lệ qua cùng stream.
    private static async Task<long> MeasureFormAsync(HttpRequest request, CancellationToken ct)
    {
        request.EnableBuffering(bufferThreshold: 64 * 1024, bufferLimit: MaxFormBytes + 1);
        byte[] buffer = new byte[32 * 1024];
        long length = 0;
        while (length <= MaxFormBytes)
        {
            int count = (int)Math.Min(buffer.Length, MaxFormBytes + 1 - length);
            int read = await request.Body.ReadAsync(buffer.AsMemory(0, count), ct);
            if (read == 0) break;
            length += read;
        }
        request.Body.Position = 0;
        return length;
    }
}
