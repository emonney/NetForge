namespace NetForge.Server.Platform.Pagination;

/// <summary>
/// Every list endpoint speaks this. Bound from the query string: reserved keys
/// (page, pageSize, sort, search) plus any other key treated as an operator filter,
/// e.g. ?price=gte:10&status=in:active,pending&sort=name:asc.
/// </summary>
public sealed record PagedRequest(
    int Page = 1,
    int PageSize = 20,
    string? Sort = null,
    string? Search = null,
    IReadOnlyDictionary<string, string>? Filters = null)
{
    public const int MaxPageSize = 200;

    private static readonly HashSet<string> Reserved =
        new(StringComparer.OrdinalIgnoreCase) { "page", "pageSize", "sort", "search" };

    public static ValueTask<PagedRequest?> BindAsync(HttpContext context)
    {
        var query = context.Request.Query;

        var page = Math.Max(1, ParseInt(query["page"], 1));
        var pageSize = Math.Clamp(ParseInt(query["pageSize"], 20), 1, MaxPageSize);

        var filters = query
            .Where(kv => !Reserved.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var request = new PagedRequest(
            page,
            pageSize,
            query["sort"].FirstOrDefault(),
            query["search"].FirstOrDefault(),
            filters.Count == 0 ? null : filters);

        return ValueTask.FromResult<PagedRequest?>(request);
    }

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) ? parsed : fallback;
}
