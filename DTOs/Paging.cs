using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.DTOs;

public class PageQuery
{
    [Range(1, 1_000_000, ErrorMessage = "page phải từ 1 đến 1000000.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "pageSize phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;
}

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount);
