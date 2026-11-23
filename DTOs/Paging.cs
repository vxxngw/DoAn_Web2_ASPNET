using System.ComponentModel.DataAnnotations;

namespace Recruitment.Api.DTOs;

public class PageQuery
{
    private int _page = 1;

    [Range(1, 1_000_000, ErrorMessage = "page phải từ 1 đến 1000000.")]
    public int Page
    {
        get => _page;
        set => _page = value;
    }

    // Bí danh PageNumber để tương thích linh hoạt giữa query param page và pageNumber
    public int? PageNumber
    {
        get => _page;
        set { if (value.HasValue && value.Value > 0) _page = value.Value; }
    }

    [Range(1, 100, ErrorMessage = "pageSize phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;
}

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount)
{
    public int PageNumber => Page;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool HasPreviousPage => HasPrevious;
    public bool HasNextPage => HasNext;
}
