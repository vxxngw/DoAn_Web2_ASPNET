using Recruitment.Api.DTOs;
using Recruitment.Api.Repositories;

namespace Recruitment.Api.Services;

public interface IAdminService
{
    Task<PagedResult<AccountSummary>> ListUsersAsync(AccountQuery filter, CancellationToken ct);
    Task SetLockAsync(int id, bool locked, CancellationToken ct);
    Task DeleteUserAsync(int id, CancellationToken ct);
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

    public async Task DeleteUserAsync(int id, CancellationToken ct)
    {
        switch (await users.DeleteAsync(id, ct))
        {
            case UserDeleteResult.Deleted:
                return;
            case UserDeleteResult.NotFound:
                throw new BusinessException(404, "Không tìm thấy tài khoản.");
            case UserDeleteResult.ProtectedAdmin:
                throw new BusinessException(403, "Không thể xóa tài khoản quản trị viên.");
            case UserDeleteResult.HasRelatedData:
                throw new BusinessException(409, "Không thể xóa tài khoản vì còn hồ sơ ứng tuyển liên quan.");
            default:
                throw new InvalidOperationException("Kết quả xóa tài khoản không xác định.");
        }
    }
}
