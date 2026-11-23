using Microsoft.EntityFrameworkCore;
using Recruitment.Api.DTOs;

namespace Recruitment.Api.Repositories;

public static class PagingExtensions
{
    // Truy vấn đã được sắp xếp trước khi gọi Skip và Take.
    public static async Task<PagedResult<T>> ToPageAsync<T>(this IQueryable<T> query, PageQuery paging, CancellationToken ct)
    {
        int total = await query.CountAsync(ct);
        int page = Math.Max(1, paging.Page);
        int pageSize = Math.Clamp(paging.PageSize, 1, 100);
        var items = await query.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }
}
