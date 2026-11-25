using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Services;

namespace Recruitment.Api.Controllers;

[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboard) : ApiControllerBase
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("admin")]
    [ProducesResponseType<AdminDashboardStats>(200)]
    public Task<AdminDashboardStats> AdminStats(CancellationToken ct) =>
        dashboard.GetAdminStatsAsync(ct);

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpGet("employer")]
    [ProducesResponseType<EmployerDashboardStats>(200)]
    public Task<EmployerDashboardStats> EmployerStats(CancellationToken ct) =>
        dashboard.GetEmployerStatsAsync(UserId, ct);
}
