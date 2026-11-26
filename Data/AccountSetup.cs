using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

public static class AccountSetup
{
    // Chỉ được gọi từ tiến trình khởi động cục bộ hoặc profile demo.
    public static async Task<bool> CreateIfMissingAsync(AppDbContext db, string email, string password,
        string fullName, UserRole role, string? companyName = null)
    {
        email = email.Trim().ToLowerInvariant();
        if (email.Length > 254 || !new EmailAddressAttribute().IsValid(email) || password.Length is < 8 or > 128)
            throw new InvalidOperationException("Cần email hợp lệ và mật khẩu dài từ 8 đến 128 ký tự.");

        if (await db.Users.AnyAsync(u => u.Email == email))
            return false;
        var user = new User { Email = email, FullName = fullName, Role = role, CompanyName = companyName };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsDuplicate(ex))
        {
            db.Entry(user).State = EntityState.Detached;
            return false;
        }
    }
}
