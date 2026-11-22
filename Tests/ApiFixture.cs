using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Tests;

[CollectionDefinition("SQL Server")]
public class SqlServerCollection : ICollectionFixture<ApiFixture> { }

public record TestAccount(int Id, string Email, HttpClient Client);

public class ApiFixture : IAsyncLifetime
{
    public const string Password = "LocalTest-2026!";
    public const string AdminEmail = "admin@example.test";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string database = $"RecruitmentTests_{Guid.NewGuid():N}";
    private readonly WebApplicationFactory<AppDbContext> factory;
    private bool databaseCreated;
    public HttpClient Public { get; private set; } = null!;
    public TestAccount Admin { get; private set; } = null!;
    public string ConnectionString => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = database,
        AttachDBFilename = Path.Combine(AppContext.BaseDirectory, database + ".mdf"),
        IntegratedSecurity = true, TrustServerCertificate = true
    }.ConnectionString;

    public ApiFixture()
    {
        string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        factory = new WebApplicationFactory<AppDbContext>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Demo:Enabled", "false");
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("ConnectionStrings:DemoConnection", ConnectionString);
            builder.UseSetting("Jwt:Key", key);
            builder.UseSetting("Logging:LogLevel:Default", "Warning");
        });
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
    }

    public AppDbContext OpenDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = OpenDb();
        await db.GetService<IRelationalDatabaseCreator>().CreateAsync();
        databaseCreated = true;
        // Chèn một tài khoản theo schema cũ để kiểm tra dữ liệu khi nâng cấp.
        await db.GetService<IMigrator>().MigrateAsync("20261122120101_InitialCreate");
        var legacy = new User { Email = "legacy@example.test", FullName = "Tài khoản cũ", Role = UserRole.Candidate };
        var hash = new PasswordHasher<User>().HashPassword(legacy, Password);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Users (Email, FullName, Role, PasswordHash) VALUES ({legacy.Email}, {legacy.FullName}, {legacy.Role.ToString()}, {hash})");
        await db.Database.MigrateAsync();
        await AccountSetup.CreateIfMissingAsync(db, AdminEmail, Password, "Admin test", UserRole.Admin);
        Public = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(database, appDb.Database.GetDbConnection().Database);
        Admin = await LoginAsync(AdminEmail);
    }

    public async Task DisposeAsync()
    {
        try { await factory.DisposeAsync(); }
        finally
        {
            if (databaseCreated)
            {
                await using var db = OpenDb();
                await db.Database.EnsureDeletedAsync();
            }
        }
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using (response)
        {
            Assert.Equal(expected, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<T>(Json))!;
        }
    }

    public static async Task ErrorAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        using (response)
        {
            Assert.Equal(expected, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        }
    }

    public static object Registration(string email, string role) => new
    {
        email, fullName = "Tài khoản kiểm thử", role, password = Password,
        companyName = role == "Employer" ? "Công ty kiểm thử" : null
    };

    public async Task<TestAccount> AccountAsync(string role)
    {
        string email = $"{Guid.NewGuid():N}@example.test";
        var response = await Public.PostAsJsonAsync("/api/auth/register", Registration(email, role));
        await ReadAsync<UserResponse>(response, HttpStatusCode.Created);
        return await LoginAsync(email);
    }

    public async Task<TestAccount> LoginAsync(string email)
    {
        var result = await ReadAsync<LoginResponse>(await Public.PostAsJsonAsync("/api/auth/login", new { email, password = Password }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        return new TestAccount(result.User.Id, email, client);
    }

    public static JobRequest NewJob(string title = "Lập trình viên", string location = "Hà Nội") =>
        new() { Title = title, Description = "Xây dựng API", Location = location, Deadline = DateTimeOffset.UtcNow.AddDays(10) };

    public static async Task<JobResponse> CreateJobAsync(TestAccount employer, JobRequest request)
    {
        using var response = await employer.Client.PostAsJsonAsync("/api/jobs", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.Equal($"/api/jobs/{created!.Id}", response.Headers.Location!.AbsolutePath);
        return await ReadAsync<JobResponse>(await employer.Client.GetAsync(response.Headers.Location));
    }
}
