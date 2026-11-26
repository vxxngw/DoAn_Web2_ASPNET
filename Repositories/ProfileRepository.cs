using Microsoft.EntityFrameworkCore;
using Recruitment.Api.Data;
using Recruitment.Api.DTOs;
using Recruitment.Api.Models;

namespace Recruitment.Api.Repositories;

public interface IProfileRepository
{
    Task<CandidateProfile?> FindAsync(int userId, CancellationToken ct);
    Task<bool> TrySaveAsync(int userId, CandidateProfileData data, CancellationToken ct);
    Task<CvInfo?> FindCvInfoAsync(int userId, CancellationToken ct);
    Task<CandidateCv?> FindCvAsync(int userId, CancellationToken ct);
    Task<bool> TrySaveCvAsync(CandidateCv cv, CancellationToken ct);
}

public class ProfileRepository(AppDbContext db) : IProfileRepository
{
    public Task<CandidateProfile?> FindAsync(int userId, CancellationToken ct) =>
        db.CandidateProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, ct);

    // Hai lần lưu hồ sơ đầu tiên cùng lúc chỉ có một bản ghi được tạo, lần còn lại trả về false.
    public async Task<bool> TrySaveAsync(int userId, CandidateProfileData data, CancellationToken ct)
    {
        var profile = await db.CandidateProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile == null)
            db.CandidateProfiles.Add(profile = new CandidateProfile { UserId = userId });
        db.Entry(profile).CurrentValues.SetValues(data);
        profile.Experiences = data.Experiences;
        profile.Educations = data.Educations;
        profile.Skills = data.Skills;
        profile.Languages = data.Languages;
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

    public Task<CvInfo?> FindCvInfoAsync(int userId, CancellationToken ct) =>
        db.CandidateCvs.Where(c => c.UserId == userId)
            .Select(c => new CvInfo(c.FileName, c.Content.Length, c.UploadedAt)).SingleOrDefaultAsync(ct);

    public Task<CandidateCv?> FindCvAsync(int userId, CancellationToken ct) =>
        db.CandidateCvs.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task<bool> TrySaveCvAsync(CandidateCv cv, CancellationToken ct)
    {
        int updated = await db.CandidateCvs.Where(c => c.UserId == cv.UserId).ExecuteUpdateAsync(set => set
            .SetProperty(c => c.FileName, cv.FileName).SetProperty(c => c.ContentType, cv.ContentType)
            .SetProperty(c => c.Content, cv.Content).SetProperty(c => c.UploadedAt, cv.UploadedAt), ct);
        if (updated == 1) return true;
        db.CandidateCvs.Add(cv);
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
}
