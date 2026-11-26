namespace Recruitment.Api.DTOs;

public record EmployerJobTotals(int Open, int Closed, int Expired, int Archived);
public record EmployerJobRow(int Id, string Title, string Status, DateTimeOffset Deadline, int Applications);
public record EmployerReport(int TotalApplications, EmployerJobTotals Totals, PagedResult<EmployerJobRow> Jobs);

public class CandidateSearch : PageQuery
{
    [System.ComponentModel.DataAnnotations.MaxLength(150)] public string? Keyword { get; set; }
}

public record PublicCandidate(int Id, string FullName, string Email, string? ProfessionalTitle, string? ProfileSummary);
