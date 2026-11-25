namespace Recruitment.Api.DTOs;

public record SavedJobResponse(
    int Id,
    int JobId,
    string CompanyName,
    string Title,
    string Location,
    DateTimeOffset Deadline,
    bool IsOpen,
    DateTimeOffset SavedAt,
    decimal? MinSalary = null,
    decimal? MaxSalary = null);
