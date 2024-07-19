using Microsoft.EntityFrameworkCore;

namespace api.Shared;

public class PageList<T>
{
    public PageList(List<T> items,
                    int page,
                    int pageSize,
                    int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage => Page * PageSize < TotalCount;
    public bool HasPreviousPage => Page > 1;
    public static async Task<PageList<T>> CreateAsync(IQueryable<T> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PageList<T>(items, page, pageSize, totalCount);
    }
    public static PageList<T> Empty() => new(new List<T>(), 0, 1, 0);
}
