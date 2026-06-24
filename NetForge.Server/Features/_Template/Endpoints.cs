using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;

namespace NetForge.Server.Features._Template;

// Canonical slice shape. This copy is inert (underscore-prefixed → never registered).
// When you copy + rename, keep the per-action RequirePermission gates; add the AuditFilter
// once auditing is wired into the pipeline.
public sealed class TemplateEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/template")
            .WithTags("Template")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List).RequirePermission(TemplatePermissions.Read);
        group.MapGet("/{id:int}", Get).RequirePermission(TemplatePermissions.Read);
        group.MapPost("/", Create).RequirePermission(TemplatePermissions.Create).AddEndpointFilter<TransactionFilter>();
        group.MapPut("/{id:int}", Update).RequirePermission(TemplatePermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapDelete("/{id:int}", Delete).RequirePermission(TemplatePermissions.Delete).AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> List(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Set<TemplateItem>().AsNoTracking().ToListAsync(ct);
        return Results.Ok(items.Select(x => x.ToDto()));
    }

    private static async Task<IResult> Get(int id, AppDbContext db, CancellationToken ct) =>
        await db.Set<TemplateItem>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) is { } item
            ? Results.Ok(item.ToDto())
            : Results.NotFound();

    private static async Task<IResult> Create(CreateTemplateItemRequest req, AppDbContext db, CancellationToken ct)
    {
        var item = new TemplateItem { Name = req.Name, Description = req.Description, CreatedAt = DateTimeOffset.UtcNow };
        db.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/template/{item.Id}", item.ToDto());
    }

    private static async Task<IResult> Update(int id, UpdateTemplateItemRequest req, AppDbContext db, CancellationToken ct)
    {
        var item = await db.Set<TemplateItem>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();

        item.Name = req.Name;
        item.Description = req.Description;
        await db.SaveChangesAsync(ct);
        return Results.Ok(item.ToDto());
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, CancellationToken ct)
    {
        var item = await db.Set<TemplateItem>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();

        db.Remove(item);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
