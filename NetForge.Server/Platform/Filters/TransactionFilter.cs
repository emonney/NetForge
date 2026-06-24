using NetForge.Server.Data;

namespace NetForge.Server.Platform.Filters;

/// <summary>
/// Wraps a write handler in a DB transaction: commit on success, roll back if the handler
/// throws. Apply per write handler (not the group) so reads don't pay the cost. The handler
/// still owns its <c>SaveChangesAsync</c> calls — this only defines the atomic boundary.
/// </summary>
public sealed class TransactionFilter(AppDbContext db) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Nested apply (already inside a transaction) — just pass through.
        if (db.Database.CurrentTransaction is not null)
            return await next(context);

        var ct = context.HttpContext.RequestAborted;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var result = await next(context); // throws propagate → using disposes → rollback
        await transaction.CommitAsync(ct);
        return result;
    }
}
