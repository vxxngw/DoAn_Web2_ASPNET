using System.Net;
using System.Text.Json;
using static Recruitment.Tests.ApiFixture;

namespace Recruitment.Tests;

[Collection("SQL Server")]
public class SwaggerTests(ApiFixture app)
{
    [Fact]
    public async Task OpenApi_documents_all_operations_with_their_auth_and_success_responses()
    {
        var spec = await ReadAsync<JsonElement>(await app.Public.GetAsync("/swagger/v1/swagger.json"));
        var paths = spec.GetProperty("paths");
        var expected = new (string Method, string Path, int Success, bool Protected)[]
        {
            ("post","/api/auth/register",201,false), ("post","/api/auth/login",200,false), ("get","/api/auth/me",200,true),
            ("put","/api/auth/me",200,true), ("put","/api/auth/me/avatar",200,true),
            ("put","/api/auth/me/password",204,true),
            ("put","/api/auth/me/candidate-profile",200,true),
            ("get","/api/profile",200,true), ("put","/api/profile",204,true),
            ("get","/api/profile/cv",200,true), ("put","/api/profile/cv",204,true),
            ("get","/api/employer/candidates",200,true), ("get","/api/employer/candidates/{id}",200,true),
            ("get","/api/employer/applications",200,true), ("get","/api/employer/report",200,true),
            ("post","/api/jobs/with-jd",201,true), ("put","/api/jobs/{id}/with-jd",204,true), ("get","/api/jobs/{id}/jd",200,false),
            ("get","/api/jobs/summary",200,false), ("get","/api/jobs",200,false), ("post","/api/jobs",201,true),
            ("get","/api/jobs/top-companies",200,false),
            ("get","/api/companies/{id}/logo",200,false), ("put","/api/companies/me/logo",204,true),
            ("get","/api/jobs/industries",200,false), ("get","/api/saved-jobs",200,true),
            ("put","/api/saved-jobs/{jobId}",204,true), ("delete","/api/saved-jobs/{jobId}",204,true),
            ("get","/api/jobs/mine",200,true), ("get","/api/jobs/{id}",200,false),
            ("put","/api/jobs/{id}",204,true), ("delete","/api/jobs/{id}",204,true),
            ("post","/api/jobs/{jobId}/applications",201,true), ("get","/api/jobs/{jobId}/applications",200,true),
            ("get","/api/applications/mine",200,true), ("get","/api/applications/{id}",200,true),
            ("get","/api/applications/{id}/cv",200,true), ("patch","/api/applications/{id}/status",204,true),
            ("get","/api/admin/users",200,true), ("patch","/api/admin/users/{id}/lock",204,true), ("get","/api/admin/jobs",200,true),
            ("get","/api/admin/users/{id}",200,true), ("put","/api/admin/users/{id}",204,true), ("delete","/api/admin/users/{id}",204,true),
            ("get","/api/admin/users/{id}/deletion-impact",200,true), ("get","/api/admin/jobs/{id}/deletion-impact",200,true),
            ("get","/api/admin/statistics",200,true)
        };
        Assert.Equal(expected.Length, paths.EnumerateObject().Sum(p => p.Value.EnumerateObject().Count()));
        foreach (var (method, path, success, requiresLogin) in expected)
        {
            var op = paths.GetProperty(path).GetProperty(method);
            Assert.True(op.GetProperty("responses").TryGetProperty(success.ToString(),out _), method + " " + path);
            if (!op.TryGetProperty("security",out var security)) { Assert.False(requiresLogin); continue; }
            Assert.Contains(security.EnumerateArray(), s => s.TryGetProperty("Bearer",out _));
            Assert.Equal(!requiresLogin, security.EnumerateArray().Any(s => !s.EnumerateObject().Any()));
            if (requiresLogin)
            {
                string route = path.Replace("{id}", "1").Replace("{jobId}", "1");
                using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), route);
                await ErrorAsync(await app.Public.SendAsync(request), HttpStatusCode.Unauthorized);
            }
        }
        var bearer = spec.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http",bearer.GetProperty("type").GetString());
        Assert.Equal("bearer",bearer.GetProperty("scheme").GetString());
    }

    [Fact]
    public async Task Documented_input_choices_binary_CV_and_business_errors_match_the_API()
    {
        var spec = await ReadAsync<JsonElement>(await app.Public.GetAsync("/swagger/v1/swagger.json"));
        var schemas = spec.GetProperty("components").GetProperty("schemas");
        var role = schemas.GetProperty("RegisterRequest").GetProperty("properties").GetProperty("role");
        Assert.Equal(new[]{"Candidate","Employer"}, role.GetProperty("enum").EnumerateArray().Select(v => v.GetString()));
        var status = schemas.GetProperty("ReviewRequest").GetProperty("properties").GetProperty("status");
        Assert.Equal(new[]{"Accepted","Rejected"}, status.GetProperty("enum").EnumerateArray().Select(v => v.GetString()));
        var version = schemas.GetProperty("UpdateJobRequest").GetProperty("properties").GetProperty("rowVersion");
        Assert.Equal(Convert.ToBase64String(new byte[8]).Length, version.GetProperty("minLength").GetInt32());
        var paths = spec.GetProperty("paths");
        var apply = paths.GetProperty("/api/jobs/{jobId}/applications").GetProperty("post");
        var multipart = apply.GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema");
        Assert.Equal("binary", multipart.GetProperty("properties").GetProperty("Cv").GetProperty("format").GetString());
        Assert.Equal(new[]{"Cv","Message"}, multipart.GetProperty("required").EnumerateArray().Select(v => v.GetString()));
        foreach (string code in new[]{"400","401","403","404","409","413","415"})
            Assert.True(apply.GetProperty("responses").GetProperty(code).GetProperty("content").TryGetProperty("application/problem+json", out _));
        var pdf = paths.GetProperty("/api/applications/{id}/cv").GetProperty("get").GetProperty("responses").GetProperty("200");
        Assert.Equal("binary",pdf.GetProperty("content").GetProperty("application/pdf").GetProperty("schema").GetProperty("format").GetString());
    }

    [Fact]
    public async Task Browser_executes_search_login_authorize_upload_and_download_in_Swagger_UI()
    {
        var employer = await app.AccountAsync("Employer");
        var candidate = await app.AccountAsync("Candidate");
        var job = await CreateJobAsync(employer, NewJob("Tin cho Swagger"));
        await RunBrowserAsync("swagger-ui.cjs", new {
            baseUrl = app.Public.BaseAddress, email = candidate.Email, employerEmail = employer.Email, password = Password, jobId = job.Id });
    }
}
