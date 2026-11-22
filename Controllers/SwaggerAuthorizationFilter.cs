using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recruitment.Api.Controllers;

public class SwaggerAuthorizationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorization = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().ToList();
        bool publicJobDetails = context.MethodInfo.DeclaringType == typeof(JobsController)
            && context.MethodInfo.Name == nameof(JobsController.Get);
        if (authorization.Count == 0 && !publicJobDetails)
            return;

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        operation.Security = [new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() }];
        if (publicJobDetails)
        {
            operation.Security.Add(new OpenApiSecurityRequirement());
            return;
        }
        var roles = authorization.Select(a => a.Roles).Where(r => r != null);
        operation.Description = roles.Any() ? $"Vai trò: {string.Join(", ", roles)}." : "Yêu cầu đăng nhập.";
        var problem = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
        foreach (var (code, description) in new[] { ("401", "Cần JWT hợp lệ."), ("403", "Không đủ quyền.") })
        {
            operation.Responses.TryAdd(code, new OpenApiResponse
            {
                Description = description,
                Content = { ["application/problem+json"] = new OpenApiMediaType { Schema = problem } }
            });
        }
    }
}
