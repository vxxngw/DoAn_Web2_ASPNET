using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;

namespace Recruitment.Api.Repositories;

public static class PagingExtensions
{
    // Truy vấn đã được sắp xếp trước khi gọi Skip và Take.
    public static async Task<PagedResult<T>> ToPageAsync<T>(this IQueryable<T> query, PageQuery paging, CancellationToken ct)
    {
        int total = await query.CountAsync(ct);
        var items = await query.Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(ct);
        return new PagedResult<T>(items, paging.Page, paging.PageSize, total);
    }
}
