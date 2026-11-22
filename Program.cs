using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Recruitment.Api.Controllers;
using Recruitment.Api.Data;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;
using Recruitment.Api.Services;

var builder = WebApplication.CreateBuilder(args);
bool demo = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demo)
    builder.Configuration["Jwt:Key"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));

var services = builder.Services;
services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
services.AddProblemDetails();
services.AddExceptionHandler<ApiExceptionHandler>();

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(demo ? "DemoConnection" : "DefaultConnection")
        ?? throw new InvalidOperationException("Thiếu cấu hình chuỗi kết nối SQL Server.")));
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IJobRepository, JobRepository>();
services.AddScoped<IApplicationRepository, ApplicationRepository>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IAdminService, AdminService>();
services.AddScoped<IJobService, JobService>();
services.AddScoped<IApplicationService, ApplicationService>();
services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

services.AddOptions<JwtSettings>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearer, settings) =>
    {
        var jwt = settings.Value;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer,
            ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = jwt.SigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };
        bearer.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                if (!int.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out int id)
                    || !Enum.TryParse<UserRole>(principal?.FindFirstValue("role"), out var role)
                    || !await users.IsActiveAsync(id, role, context.HttpContext.RequestAborted))
                    context.Fail("Tài khoản không còn quyền sử dụng phiên này.");
            }
        };
    });
services.AddAuthorization();

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API tuyển dụng và kết nối việc làm", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Dán giá trị accessToken từ API đăng nhập."
    });
    options.OperationFilter<SwaggerAuthorizationFilter>();
    options.SchemaFilter<SwaggerRequestSchemaFilter>();
});

var app = builder.Build();
if (args.Contains("--create-admin"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    string email = builder.Configuration["AdminSetup:Email"] ?? throw new InvalidOperationException("Thiếu AdminSetup:Email.");
    string password = builder.Configuration["AdminSetup:Password"] ?? throw new InvalidOperationException("Thiếu AdminSetup:Password.");
    bool created = await AccountSetup.CreateIfMissingAsync(db, email, password, "Quản trị viên", UserRole.Admin);
    Console.WriteLine(created ? "Đã tạo quản trị viên." : "Email đã tồn tại. Hãy dùng email mới để tạo quản trị viên.");
    Environment.ExitCode = created ? 0 : 1;
    return;
}
if (demo)
{
    using var scope = app.Services.CreateScope();
    await DemoData.InitializeAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    int status = context.HttpContext.Response.StatusCode;
    string title = status switch
    {
        401 => "Cần đăng nhập bằng JWT hợp lệ.",
        403 => "Tài khoản không có quyền thực hiện thao tác này.",
        404 => "Không tìm thấy tài nguyên.",
        405 => "Phương thức HTTP không được hỗ trợ.",
        413 => "Dữ liệu tải lên quá lớn.",
        415 => "Định dạng nội dung không được hỗ trợ.",
        _ => "Yêu cầu không thành công."
    };
    await Results.Problem(statusCode: status, title: title).ExecuteAsync(context.HttpContext);
});
if (!demo)
    app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
