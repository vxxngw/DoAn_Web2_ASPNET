using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var error = exception switch
        {
            BusinessException business => business,
            DbUpdateConcurrencyException => new BusinessException(409, "Tin vừa thay đổi. Vui lòng tải lại."),
            DbUpdateException { InnerException: SqlException { Number: 547 or 1205 } }
                or SqlException { Number: 547 or 1205 } =>
                new BusinessException(409, "Dữ liệu liên quan vừa thay đổi. Vui lòng tải lại và thử lại."),
            BadHttpRequestException badRequest when badRequest.StatusCode == 413 =>
                new BusinessException(413, "Dữ liệu tải lên quá lớn."),
            _ => null
        };
        if (error == null)
            return false;

        context.Response.StatusCode = error.StatusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Message,
            Instance = context.Request.Path
        }, contentType: "application/problem+json", cancellationToken: ct, options: null);
        return true;
    }
}
