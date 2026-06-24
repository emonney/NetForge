namespace NetForge.Server.Platform.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrev)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<T>(items, page, pageSize, totalItems, totalPages, page < totalPages, page > 1);
    }
}
