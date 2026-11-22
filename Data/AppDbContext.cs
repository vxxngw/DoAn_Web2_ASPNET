using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var user = model.Entity<User>();
        user.HasIndex(u => u.Email).IsUnique();
        user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

        var job = model.Entity<Job>();
        job.HasOne(j => j.Employer).WithMany().HasForeignKey(j => j.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);
        job.HasIndex(j => new { j.IsOpen, j.Deadline });

        var application = model.Entity<JobApplication>();
        application.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        application.HasIndex(a => new { a.JobId, a.CandidateId }).IsUnique();
        application.HasOne(a => a.Job).WithMany().HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);
        application.HasOne(a => a.Candidate).WithMany().HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
