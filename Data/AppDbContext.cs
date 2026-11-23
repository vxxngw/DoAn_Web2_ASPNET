using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var user = model.Entity<User>();
        user.HasIndex(u => u.Email).IsUnique();
        user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

    }
}
