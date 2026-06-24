using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace NetForge.Server.Platform.Pagination;

/// <summary>
/// Applies a <see cref="PagedRequest"/> (filters, search, sort, paging) to an
/// <see cref="IQueryable{T}"/> against a <see cref="QuerySpec{T}"/> allowlist, building
/// EF-translatable predicate/order expressions. Unknown or unparseable filters are skipped.
/// </summary>
public static class QueryableExtensions
{
    private static readonly HashSet<string> KnownOps = new(StringComparer.OrdinalIgnoreCase)
        { "eq", "ne", "gt", "gte", "lt", "lte", "in", "nin", "contains", "startswith", "endswith" };

    public static async Task<PagedResult<TOut>> ToPagedResultAsync<T, TOut>(
        this IQueryable<T> source,
        PagedRequest request,
        QuerySpec<T> spec,
        Func<T, TOut> map,
        CancellationToken cancellationToken = default)
    {
        source = ApplyFilters(source, request, spec);
        source = ApplySearch(source, request, spec);

        var total = await source.CountAsync(cancellationToken);

        source = ApplySort(source, request, spec);
        var items = await source
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(map).ToList();
        return PagedResult<TOut>.Create(mapped, request.Page, request.PageSize, total);
    }

    private static IQueryable<T> ApplyFilters<T>(IQueryable<T> source, PagedRequest request, QuerySpec<T> spec)
    {
        if (request.Filters is null) return source;

        foreach (var (key, raw) in request.Filters)
        {
            if (!spec.Filters.TryGetValue(key, out var selector)) continue; // ignore non-allowlisted fields
            if (BuildPredicate<T>(selector, raw) is { } predicate) source = source.Where(predicate);
        }

        return source;
    }

    private static IQueryable<T> ApplySearch<T>(IQueryable<T> source, PagedRequest request, QuerySpec<T> spec)
    {
        if (string.IsNullOrWhiteSpace(request.Search) || spec.SearchFields.Count == 0) return source;

        // Case-insensitive substring match: lower() both sides so "chair" finds "Chair". EF translates
        // ToLower() to the provider's lower(); SQLite/Postgres otherwise match substrings case-sensitively.
        var term = request.Search.Trim().ToLowerInvariant();
        var param = Expression.Parameter(typeof(T), "x");
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

        Expression? combined = null;
        foreach (var field in spec.SearchFields)
        {
            var body = Rebind(field, param);
            var clause = Expression.AndAlso(
                Expression.NotEqual(body, Expression.Constant(null, typeof(string))),
                Expression.Call(Expression.Call(body, toLower), contains, Expression.Constant(term)));
            combined = combined is null ? clause : Expression.OrElse(combined, clause);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(combined!, param);
        return source.Where(lambda);
    }

    private static IQueryable<T> ApplySort<T>(IQueryable<T> source, PagedRequest request, QuerySpec<T> spec)
    {
        IOrderedQueryable<T>? ordered = null;

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            foreach (var token in request.Sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var bits = token.Split(':', 2);
                var name = bits[0].Trim();
                var desc = bits.Length > 1 && bits[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
                if (!spec.Sorts.TryGetValue(name, out var selector)) continue;
                ordered = ApplyOrder(source, ordered, selector, desc);
            }
        }

        if (ordered is null)
        {
            // Skip/Take needs a stable order. Fall back to the configured default, else the first sortable field.
            var selector = spec.Default is { } d && spec.Sorts.TryGetValue(d.Name, out var s)
                ? s
                : spec.Sorts.Values.FirstOrDefault();
            if (selector is null) return source;
            ordered = ApplyOrder(source, null, selector, spec.Default?.Desc ?? false);
        }

        return ordered;
    }

    private static IOrderedQueryable<T> ApplyOrder<T>(
        IQueryable<T> source, IOrderedQueryable<T>? current, LambdaExpression selector, bool desc)
    {
        var method = (current is null, desc) switch
        {
            (true, false) => "OrderBy",
            (true, true) => "OrderByDescending",
            (false, false) => "ThenBy",
            (false, true) => "ThenByDescending",
        };

        var call = Expression.Call(
            typeof(Queryable), method, [typeof(T), selector.Body.Type],
            (current ?? source).Expression, Expression.Quote(selector));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    private static Expression<Func<T, bool>>? BuildPredicate<T>(LambdaExpression selector, string raw)
    {
        var (op, operand) = ParseOperator(raw);
        var param = Expression.Parameter(typeof(T), "x");
        var member = Rebind(selector, param);
        var nullable = Nullable.GetUnderlyingType(member.Type) is not null || !member.Type.IsValueType;

        Expression? predicate = op switch
        {
            "null" => nullable ? Expression.Equal(member, Expression.Constant(null, member.Type)) : null,
            "notnull" => nullable ? Expression.NotEqual(member, Expression.Constant(null, member.Type)) : null,
            "in" => BuildIn(member, operand, negate: false),
            "nin" => BuildIn(member, operand, negate: true),
            "contains" or "startswith" or "endswith" => BuildStringCall(member, op, operand),
            _ => BuildComparison(member, op, operand),
        };

        return predicate is null ? null : Expression.Lambda<Func<T, bool>>(predicate, param);
    }

    private static Expression? BuildComparison(Expression member, string op, string operand)
    {
        var underlying = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

        // Relational operators only make sense for ordered (non-string) types.
        if (op is not ("eq" or "ne") && underlying == typeof(string)) return null;

        var value = ConvertOperand(operand, underlying);
        if (value is null) return null;

        Expression constant = Expression.Constant(value, underlying);
        if (underlying != member.Type) constant = Expression.Convert(constant, member.Type);

        return op switch
        {
            "eq" => Expression.Equal(member, constant),
            "ne" => Expression.NotEqual(member, constant),
            "gt" => Expression.GreaterThan(member, constant),
            "gte" => Expression.GreaterThanOrEqual(member, constant),
            "lt" => Expression.LessThan(member, constant),
            "lte" => Expression.LessThanOrEqual(member, constant),
            _ => null,
        };
    }

    private static Expression? BuildStringCall(Expression member, string op, string operand)
    {
        if (member.Type != typeof(string)) return null;

        var name = op switch
        {
            "contains" => nameof(string.Contains),
            "startswith" => nameof(string.StartsWith),
            "endswith" => nameof(string.EndsWith),
            _ => null,
        };
        if (name is null) return null;

        var method = typeof(string).GetMethod(name, [typeof(string)])!;
        return Expression.AndAlso(
            Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
            Expression.Call(member, method, Expression.Constant(operand)));
    }

    private static Expression? BuildIn(Expression member, string operand, bool negate)
    {
        var underlying = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
        var listType = typeof(List<>).MakeGenericType(underlying);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var part in operand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (ConvertOperand(part, underlying) is { } v) list.Add(v);

        if (list.Count == 0) return null;

        var containsMethod = listType.GetMethod("Contains", [underlying])!;
        var memberExpr = member.Type == underlying ? member : Expression.Convert(member, underlying);
        Expression call = Expression.Call(Expression.Constant(list, listType), containsMethod, memberExpr);
        return negate ? Expression.Not(call) : call;
    }

    private static object? ConvertOperand(string raw, Type targetType)
    {
        try
        {
            if (targetType == typeof(string)) return raw;
            if (targetType == typeof(Guid)) return Guid.Parse(raw);
            if (targetType.IsEnum) return Enum.Parse(targetType, raw, ignoreCase: true);
            if (targetType == typeof(bool)) return bool.Parse(raw);
            if (targetType == typeof(DateTime))
                return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null; // unparseable operand → skip this filter rather than 500
        }
    }

    private static (string Op, string Operand) ParseOperator(string raw)
    {
        if (raw.Equals("null", StringComparison.OrdinalIgnoreCase)) return ("null", "");
        if (raw.Equals("notnull", StringComparison.OrdinalIgnoreCase)) return ("notnull", "");

        var idx = raw.IndexOf(':');
        if (idx > 0 && KnownOps.Contains(raw[..idx]))
            return (raw[..idx].ToLowerInvariant(), raw[(idx + 1)..]);

        return ("eq", raw);
    }

    private static Expression Rebind(LambdaExpression selector, ParameterExpression to) =>
        new ParameterReplacer(selector.Parameters[0], to).Visit(selector.Body)!;

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
