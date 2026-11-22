using Recruitment.Api.DTOs;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IAdminService
{
    Task<PagedResult<AccountSummary>> ListUsersAsync(AccountQuery filter, CancellationToken ct);
    Task SetLockAsync(int id, bool locked, CancellationToken ct);
}

public class AdminService(IUserRepository users) : IAdminService
{
    public Task<PagedResult<AccountSummary>> ListUsersAsync(AccountQuery filter, CancellationToken ct) =>
        users.ListAsync(filter, ct);

    public async Task SetLockAsync(int id, bool locked, CancellationToken ct)
    {
        if (await users.SetLockAsync(id, locked, ct) == 1)
            return;
        if (await users.FindAsync(id, ct) == null)
            throw new BusinessException(404, "Không tìm thấy tài khoản.");
        throw new BusinessException(403, "Chỉ có thể khóa hoặc mở khóa ứng viên và nhà tuyển dụng.");
    }
}
