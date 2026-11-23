using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindAsync(int id, CancellationToken ct);
    Task<bool> TryAddAsync(User user, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindAsync(int id, CancellationToken ct) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == id, ct);

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
