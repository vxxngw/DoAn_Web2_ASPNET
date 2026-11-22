using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface IUserRepository
{
    Task<PagedResult<AccountSummary>> ListAsync(AccountQuery filter, CancellationToken ct);
    Task<bool> IsActiveAsync(int id, UserRole role, CancellationToken ct);
    Task<int> SetLockAsync(int id, bool locked, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindAsync(int id, CancellationToken ct);
    Task<bool> TryAddAsync(User user, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<PagedResult<AccountSummary>> ListAsync(AccountQuery filter, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(u => u.Email.Contains(keyword) || u.FullName.Contains(keyword));
        }
        return query.OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(u => new AccountSummary(u.Id, u.FullName, u.Email, u.Role, u.IsLocked))
            .ToPageAsync(filter, ct);
    }

    public Task<bool> IsActiveAsync(int id, UserRole role, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == id && u.Role == role && !u.IsLocked, ct);

    public Task<int> SetLockAsync(int id, bool locked, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && u.Role != UserRole.Admin)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.IsLocked, locked), ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindAsync(int id, CancellationToken ct) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);

    public async Task<bool> TryAddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            return false;
        }
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
