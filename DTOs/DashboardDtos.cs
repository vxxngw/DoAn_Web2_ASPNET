namespace Recruitment.Api.DTOs;

public record LocationStat(string Location, int JobCount);
public record StatusStat(string Status, int Count);

public record AdminDashboardStats(
    int TotalCandidates,
    int TotalEmployers,
    int TotalJobs,
    int OpenJobs,
    int TotalApplications,
    int AcceptedApplications,
    List<LocationStat> TopLocations,
    List<StatusStat> ApplicationStatusBreakdown);

public record EmployerJobStat(int JobId, string Title, int ApplicationCount, bool IsOpen);

public record EmployerDashboardStats(
    int TotalJobs,
    int OpenJobs,
    int TotalApplications,
    int PendingApplications,
    int AcceptedApplications,
    int RejectedApplications,
    List<EmployerJobStat> TopJobs);
