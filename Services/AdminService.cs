using Recruitment.Api.DTOs;
using Recruitment.Api.Models;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IAdminService
{
    Task<PagedResult<AccountSummary>> ListUsersAsync(AccountQuery filter, CancellationToken ct);
    Task SetLockAsync(int id, bool locked, CancellationToken ct);
    Task<AccountSummary> GetUserAsync(int id, CancellationToken ct);
    Task EditUserAsync(int id, EditAccountRequest request, CancellationToken ct);
    Task DeleteUserAsync(int id, CancellationToken ct);
    Task<DeletionImpact> UserImpactAsync(int id, CancellationToken ct);
    Task<DeletionImpact> JobImpactAsync(int id, CancellationToken ct);
    Task<AdminStatistics> StatisticsAsync(StatisticsQuery query, CancellationToken ct);
}

public class AdminService(IUserRepository users, IAdminRepository admin) : IAdminService
{
    public Task<PagedResult<AccountSummary>> ListUsersAsync(AccountQuery filter, CancellationToken ct) =>
        users.ListAsync(filter, ct);

    public async Task<AccountSummary> GetUserAsync(int id, CancellationToken ct)
    {
        var u = await users.FindAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");
        return new AccountSummary(u.Id, u.FullName, u.Email, u.Role, u.IsLocked, u.CompanyName);
    }

    public async Task EditUserAsync(int id, EditAccountRequest request, CancellationToken ct)
    {
        var account = await GetUserAsync(id, ct);
        if (account.Role == UserRole.Admin)
            throw new BusinessException(403, "Chỉ có thể quản lý thông tin ứng viên và nhà tuyển dụng.");
        string? company = request.CompanyName?.Trim();
        if (account.Role == UserRole.Employer && string.IsNullOrEmpty(company))
            throw new BusinessException(400, "Nhà tuyển dụng phải có tên công ty.");
        if (account.Role == UserRole.Candidate && !string.IsNullOrEmpty(company))
            throw new BusinessException(400, "Chỉ nhà tuyển dụng có tên công ty.");
        var result = await users.UpdateProfileAsync(id, request.FullName.Trim(), request.Email.Trim().ToLowerInvariant(),
            account.Role == UserRole.Employer ? company : null, ct);
        if (result == ProfileUpdateResult.EmailInUse)
            throw new BusinessException(409, "Email này đã được sử dụng.");
        if (result == ProfileUpdateResult.Missing)
            throw new BusinessException(404, "Không tìm thấy tài khoản.");
    }

    public async Task SetLockAsync(int id, bool locked, CancellationToken ct)
    {
        if (await users.SetLockAsync(id, locked, ct) == 1)
            return;
        if (await users.FindAsync(id, ct) == null)
            throw new BusinessException(404, "Không tìm thấy tài khoản.");
        throw new BusinessException(403, "Chỉ có thể khóa hoặc mở khóa ứng viên và nhà tuyển dụng.");
    }

    public async Task DeleteUserAsync(int id, CancellationToken ct)
    {
        var account = await GetUserAsync(id, ct);
        if (account.Role == UserRole.Admin)
            throw new BusinessException(403, "Không thể xóa tài khoản quản trị viên.");
        if (!await admin.DeleteUserAsync(id, ct))
            throw new BusinessException(404, "Không tìm thấy tài khoản.");
    }

    public async Task<DeletionImpact> UserImpactAsync(int id, CancellationToken ct) =>
        await admin.UserImpactAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tài khoản.");

    public async Task<DeletionImpact> JobImpactAsync(int id, CancellationToken ct) =>
        await admin.JobImpactAsync(id, ct) ?? throw new BusinessException(404, "Không tìm thấy tin.");

    public Task<AdminStatistics> StatisticsAsync(StatisticsQuery query, CancellationToken ct)
    {
        if (query.Period == ReportPeriod.All)
            return admin.StatisticsAsync(query.Period, null, null, ct);
        var date = query.Date ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date);
        if (date.Year is < 2 or > 9998)
            throw new BusinessException(400, "Ngày báo cáo phải nằm trong khoảng năm 0002 đến 9998.");
        var (start, days) = query.Period switch
        {
            ReportPeriod.Day => (date, 1),
            ReportPeriod.Week => (date.AddDays(-((int)date.DayOfWeek + 6) % 7), 7),
            ReportPeriod.Month => (new DateOnly(date.Year, date.Month, 1), DateTime.DaysInMonth(date.Year, date.Month)),
            _ => throw new BusinessException(400, "Kỳ báo cáo không hợp lệ.")
        };
        return admin.StatisticsAsync(query.Period, start, start.AddDays(days), ct);
    }
}
