using System.Linq.Expressions;

namespace NetForge.Server.Platform.Pagination;

/// <summary>
/// Per-slice allowlist of filterable / sortable / searchable fields. Only fields
/// registered here can be referenced from the query string — anything else is ignored,
/// so clients can't probe arbitrary columns. Build one per list endpoint.
/// </summary>
public sealed class QuerySpec<T>
{
    internal Dictionary<string, LambdaExpression> Filters { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, LambdaExpression> Sorts { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal List<LambdaExpression> SearchFields { get; } = [];
    internal (string Name, bool Desc)? Default { get; private set; }

    /// <summary>Allow <paramref name="name"/> to be both filtered and sorted.</summary>
    public QuerySpec<T> Allow<TProp>(string name, Expression<Func<T, TProp>> selector)
    {
        Filters[name] = selector;
        Sorts[name] = selector;
        return this;
    }

    /// <summary>Allow filtering only (not sorting) on <paramref name="name"/>.</summary>
    public QuerySpec<T> FilterOnly<TProp>(string name, Expression<Func<T, TProp>> selector)
    {
        Filters[name] = selector;
        return this;
    }

    /// <summary>Include a string field in free-text <c>search</c> matching.</summary>
    public QuerySpec<T> Searchable(Expression<Func<T, string?>> selector)
    {
        SearchFields.Add(selector);
        return this;
    }

    /// <summary>Deterministic order applied when the request specifies no valid sort.</summary>
    public QuerySpec<T> DefaultSort(string name, bool descending = false)
    {
        Default = (name, descending);
        return this;
    }
}
