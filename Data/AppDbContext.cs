using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Models;

namespace Recruitment.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateCv> CandidateCvs => Set<CandidateCv>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var user = model.Entity<User>();
        user.HasIndex(u => u.Email).IsUnique();
        user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

        var job = model.Entity<Job>();
        job.HasOne(j => j.Employer).WithMany().HasForeignKey(j => j.EmployerId)
            .OnDelete(DeleteBehavior.Restrict);
        job.HasIndex(j => new { j.IsOpen, j.Deadline });

        var saved = model.Entity<SavedJob>();
        saved.HasKey(s => new { s.CandidateId, s.JobId });
        saved.HasOne<User>().WithMany().HasForeignKey(s => s.CandidateId).OnDelete(DeleteBehavior.Cascade);
        saved.HasOne(s => s.Job).WithMany().HasForeignKey(s => s.JobId).OnDelete(DeleteBehavior.Cascade);

        var application = model.Entity<JobApplication>();
        application.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        application.HasIndex(a => new { a.JobId, a.CandidateId }).IsUnique();
        application.HasOne(a => a.Job).WithMany().HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);
        application.HasOne(a => a.Candidate).WithMany().HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Các danh sách trong hồ sơ được lưu thành cột JSON, xóa tài khoản thì xóa luôn hồ sơ và CV.
        var profile = model.Entity<CandidateProfile>();
        profile.HasOne<User>().WithOne().HasForeignKey<CandidateProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        profile.OwnsMany(p => p.Experiences, list => list.ToJson());
        profile.OwnsMany(p => p.Educations, list => list.ToJson());
        profile.OwnsMany(p => p.Skills, list => list.ToJson());
        profile.OwnsMany(p => p.Languages, list => list.ToJson());
        model.Entity<CandidateCv>().HasOne<User>().WithOne().HasForeignKey<CandidateCv>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
