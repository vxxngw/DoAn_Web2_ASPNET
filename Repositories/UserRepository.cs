using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public enum ProfileUpdateResult { Updated, Missing, EmailInUse }

public interface IUserRepository
{
    Task<int> UpdateCandidateProfileAsync(int id, CandidateProfileRequest request, CancellationToken ct);
    Task<PagedResult<PublicCandidate>> PublicCandidatesAsync(CandidateSearch filter, CancellationToken ct);
    Task<PublicCandidate?> PublicCandidateAsync(int id, CancellationToken ct);
    Task<PagedResult<AccountSummary>> ListAsync(AccountQuery filter, CancellationToken ct);
    Task<bool> IsActiveAsync(int id, UserRole role, CancellationToken ct);
    Task<int> SetLockAsync(int id, bool locked, CancellationToken ct);
    Task<ProfileUpdateResult> UpdateProfileAsync(int id, string name, string email, string? company, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindAsync(int id, CancellationToken ct);
    Task<int> UpdateOwnDetailsAsync(int id, string name, string? company, CancellationToken ct);
    Task<int> SetAvatarAsync(int id, byte[] image, CancellationToken ct);
    Task<int> SetCompanyLogoAsync(int id, byte[] image, CancellationToken ct);
    Task<byte[]?> GetCompanyLogoAsync(int id, CancellationToken ct);
    Task<bool> TryAddAsync(User user, CancellationToken ct);
    Task<bool> TryChangePasswordHashAsync(int id, string previousHash, string newHash, CancellationToken ct);
}

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<int> UpdateCandidateProfileAsync(int id, CandidateProfileRequest request, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && u.Role == UserRole.Candidate && !u.IsLocked).ExecuteUpdateAsync(set => set
            .SetProperty(u => u.IsProfilePublic, request.IsProfilePublic!.Value)
            .SetProperty(u => u.ProfessionalTitle, request.ProfessionalTitle)
            .SetProperty(u => u.ProfileSummary, request.ProfileSummary), ct);

    private IQueryable<User> PublicProfiles() =>
        db.Users.AsNoTracking().Where(u => u.Role == UserRole.Candidate && u.IsProfilePublic && !u.IsLocked);

    private static readonly System.Linq.Expressions.Expression<Func<User, PublicCandidate>> PublicProfile =
        u => new PublicCandidate(u.Id, u.FullName, u.Email, u.ProfessionalTitle, u.ProfileSummary);

    public Task<PagedResult<PublicCandidate>> PublicCandidatesAsync(CandidateSearch filter, CancellationToken ct)
    {
        var users = PublicProfiles();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim();
            users = users.Where(u => u.FullName.Contains(term) || u.Email.Contains(term) || (u.ProfessionalTitle != null && u.ProfessionalTitle.Contains(term)));
        }
        return users.OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(PublicProfile).ToPageAsync(filter, ct);
    }

    public Task<PublicCandidate?> PublicCandidateAsync(int id, CancellationToken ct) =>
        PublicProfiles().Where(u => u.Id == id).Select(PublicProfile).SingleOrDefaultAsync(ct);

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

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, ct);

    public async Task<ProfileUpdateResult> UpdateProfileAsync(int id, string name, string email, string? company, CancellationToken ct)
    {
        try
        {
            int updated = await db.Users.Where(u => u.Id == id && u.Role != UserRole.Admin)
                .ExecuteUpdateAsync(set => set.SetProperty(u => u.FullName, name)
                    .SetProperty(u => u.Email, email).SetProperty(u => u.CompanyName, company), ct);
            return updated == 1 ? ProfileUpdateResult.Updated : ProfileUpdateResult.Missing;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return ProfileUpdateResult.EmailInUse;
        }
    }

    public Task<User?> FindAsync(int id, CancellationToken ct) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);

    public Task<int> UpdateOwnDetailsAsync(int id, string name, string? company, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && !u.IsLocked).ExecuteUpdateAsync(set =>
            set.SetProperty(u => u.FullName, name).SetProperty(u => u.CompanyName, company), ct);

    public Task<int> SetAvatarAsync(int id, byte[] image, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && !u.IsLocked).ExecuteUpdateAsync(set => set.SetProperty(u => u.AvatarImage, image), ct);

    public Task<int> SetCompanyLogoAsync(int id, byte[] image, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && u.Role == UserRole.Employer && !u.IsLocked)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.CompanyLogo, image), ct);

    public Task<byte[]?> GetCompanyLogoAsync(int id, CancellationToken ct) =>
        db.Users.Where(u => u.Id == id && u.Role == UserRole.Employer && !u.IsLocked)
            .Select(u => u.CompanyLogo).SingleOrDefaultAsync(ct);

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

    public async Task<bool> TryChangePasswordHashAsync(int id, string previousHash, string newHash, CancellationToken ct)
    {
        int updated = await db.Users.Where(u => u.Id == id && !u.IsLocked && u.PasswordHash == previousHash)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.PasswordHash, newHash), ct);
        return updated == 1;
    }
}
