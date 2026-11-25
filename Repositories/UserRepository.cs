using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public enum UserDeleteResult { Deleted, NotFound, ProtectedAdmin, HasRelatedData }

public interface IUserRepository
{
    Task<PagedResult<AccountSummary>> ListAsync(AccountQuery filter, CancellationToken ct);
    Task<bool> IsActiveAsync(int id, UserRole role, CancellationToken ct);
    Task<int> SetLockAsync(int id, bool locked, CancellationToken ct);
    Task<UserDeleteResult> DeleteAsync(int id, CancellationToken ct);
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
        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(u => u.Email.Contains(keyword) || u.FullName.Contains(keyword)
                || (u.CompanyName != null && u.CompanyName.Contains(keyword)));
        }
        return query.OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(u => new AccountSummary(u.Id, u.FullName, u.Email, u.Role, u.IsLocked, u.CompanyName))
            .ToPageAsync(filter, ct);
    }

    public Task<bool> IsActiveAsync(int id, UserRole role, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == id && u.Role == role && !u.IsLocked, ct);

    public Task<int> SetLockAsync(int id, bool locked, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && u.Role != UserRole.Admin)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.IsLocked, locked), ct);

    public async Task<UserDeleteResult> DeleteAsync(int id, CancellationToken ct)
    {
        var role = await db.Users.Where(u => u.Id == id).Select(u => (UserRole?)u.Role).SingleOrDefaultAsync(ct);
        if (role == null)
            return UserDeleteResult.NotFound;
        if (role == UserRole.Admin)
            return UserDeleteResult.ProtectedAdmin;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (role == UserRole.Employer)
            {
                await db.SavedJobs.Where(s => s.Job.EmployerId == id).ExecuteDeleteAsync(ct);
                await db.Applications.Where(a => a.Job.EmployerId == id).ExecuteDeleteAsync(ct);
                await db.Jobs.Where(j => j.EmployerId == id).ExecuteDeleteAsync(ct);
            }
            else if (role == UserRole.Candidate)
            {
                await db.SavedJobs.Where(s => s.CandidateId == id).ExecuteDeleteAsync(ct);
            }

            int deleted = await db.Users.Where(u => u.Id == id && u.Role == role).ExecuteDeleteAsync(ct);
            if (deleted == 0)
                return UserDeleteResult.NotFound;

            await transaction.CommitAsync(ct);
            return UserDeleteResult.Deleted;
        }
        catch (SqlException ex) when (DatabaseErrors.IsForeignKeyViolation(ex))
        {
            await transaction.RollbackAsync(ct);
            return UserDeleteResult.HasRelatedData;
        }
    }

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
