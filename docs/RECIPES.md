# NetForge — Recipes

Long-form, copy-pasteable how-tos for the common extension tasks. The short version of each lives in [../CLAUDE.md](../CLAUDE.md); the one-screen cheat-sheet is [CONVENTIONS.md](CONVENTIONS.md).

**The golden rule:** almost every recipe starts by copying `_Template/` (backend) or `_template/` (frontend). The scaffolding is the canonical shape — don't hand-roll from memory.

**Edition note:** some recipes cover optional subsystems (search, webhooks, the dashboard, export, multi-tenancy) that a trimmed/Community scaffold may not include. The full online docs — which mark each feature Community or Pro — are at <https://docs.netforge.ebenmonney.com>.

## Contents

1. [Add a backend feature (vertical slice)](#1-add-a-backend-feature-vertical-slice)
2. [Add a frontend screen](#2-add-a-frontend-screen)
3. [Add a permission](#3-add-a-permission)
4. [Build a list end-to-end (DataGrid)](#4-build-a-list-end-to-end-datagrid)
5. [Report an error (ProblemDetails)](#5-report-an-error-problemdetails)
6. [Add a setting](#6-add-a-setting)
7. [Add a background job](#7-add-a-background-job)
8. [Make a slice findable in ⌘K (search provider)](#8-make-a-slice-findable-in-k-search-provider)
9. [Emit a webhook event](#9-emit-a-webhook-event)
10. [Add a dashboard widget](#10-add-a-dashboard-widget)
11. [Soft delete + restore](#11-soft-delete--restore)
12. [Optimistic concurrency](#12-optimistic-concurrency)
13. [Tenant-scope an entity](#13-tenant-scope-an-entity)
14. [Store money](#14-store-money)
15. [Export & import (CSV / Excel / PDF)](#15-export--import-csv--excel--pdf)
16. [Add an activity timeline + comments to a detail page](#16-add-an-activity-timeline--comments-to-a-detail-page)
17. [Add a language](#17-add-a-language)
18. [Add an OAuth provider](#18-add-an-oauth-provider)
19. [Add a health check](#19-add-a-health-check)
20. [Rate-limit or version a slice](#20-rate-limit-or-version-a-slice)
21. [Test a slice](#21-test-a-slice)
22. [Announce a release / add an onboarding step](#22-announce-a-release--add-an-onboarding-step)

---

## 1. Add a backend feature (vertical slice)

A slice is six files under `Features/{Domain}/`. Start by copying the template:

```bash
cp -r NetForge.Server/Features/_Template NetForge.Server/Features/Projects
```

Then rename `Template` → `Project` (and `template` → `project`) throughout the copied files. Here's what each file becomes — the `_Template` versions are the canonical shape:

**`Models.cs`** — entity + DTO + request records:

```csharp
namespace NetForge.Server.Features.Projects;

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record ProjectDto(int Id, string Name, string? Description, DateTimeOffset CreatedAt);
public record CreateProjectRequest(string Name, string? Description);
public record UpdateProjectRequest(string Name, string? Description);
```

**`Permissions.cs`** — one constant per action (`[Description]` feeds the admin catalog UI):

```csharp
using System.ComponentModel;
namespace NetForge.Server.Features.Projects;

public static class ProjectPermissions
{
    [Description("View projects")]    public const string Read   = "projects.read";
    [Description("Create projects")]  public const string Create = "projects.create";
    [Description("Edit projects")]    public const string Update = "projects.update";
    [Description("Delete projects")]  public const string Delete = "projects.delete";
}
```

**`Validators.cs`** — `internal sealed` validator per request (the `ValidationFilter` runs them automatically):

```csharp
using FluentValidation;
namespace NetForge.Server.Features.Projects;

internal sealed class CreateProjectValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
```

**`Mappings.cs`** — static extension, no AutoMapper:

```csharp
namespace NetForge.Server.Features.Projects;

internal static class ProjectMappings
{
    public static ProjectDto ToDto(this Project e) => new(e.Id, e.Name, e.Description, e.CreatedAt);
}
```

**`EfConfig.cs`** — `IEntityTypeConfiguration<T>`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace NetForge.Server.Features.Projects;

internal sealed class ProjectConfig : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("Projects");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
    }
}
```

**`Endpoints.cs`** — the `IFeatureEndpoints` impl. Note the filter placement: validation/performance per group, **transaction per write handler**:

```csharp
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Data;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Filters;

namespace NetForge.Server.Features.Projects;

public sealed class ProjectEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<PerformanceFilter>();

        group.MapGet("/", List).RequirePermission(ProjectPermissions.Read);
        group.MapGet("/{id:int}", Get).RequirePermission(ProjectPermissions.Read);
        group.MapPost("/", Create).RequirePermission(ProjectPermissions.Create).AddEndpointFilter<TransactionFilter>();
        group.MapPut("/{id:int}", Update).RequirePermission(ProjectPermissions.Update).AddEndpointFilter<TransactionFilter>();
        group.MapDelete("/{id:int}", Delete).RequirePermission(ProjectPermissions.Delete).AddEndpointFilter<TransactionFilter>();
    }

    private static async Task<IResult> Get(int id, AppDbContext db, CancellationToken ct) =>
        await db.Set<Project>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) is { } p
            ? Results.Ok(p.ToDto())
            : throw new NotFoundException(nameof(Project), id);   // → 404 ProblemDetails
    // ...List/Create/Update/Delete as in _Template
}
```

Finally, add the migration:

```bash
dotnet ef migrations add AddProjects --project NetForge.Server
```

The slice is now live — **you never touched `Program.cs`.** `MapAllFeatures()` found it by reflection. Assign the new permissions to a role at `/admin/roles`. (For the frontend, hand-author the typed API module — see recipe 2.)

> Ordering: if a slice must register before/after others, add `[FeatureOrder(n)]` to the endpoints class (default 100).

---

## 2. Add a frontend screen

Routes are files under `src/pages/`. The path tree is the URL tree (generouted). Copy the template:

```bash
cp -r netforge.client/src/pages/_template "netforge.client/src/pages/(app)/projects"
```

A screen folder holds:

- **`index.tsx`** — the screen, default export. Optional same-file `Pending` (loading) and `Catch` (error) exports.
- **`meta.ts`** — `{ title, permissions }`. The shell reads `permissions` to gate the route + nav.
- **`_layout.tsx`** — optional; wraps this subtree.

```ts
// src/pages/(app)/projects/meta.ts
export const meta = {
  title: 'Projects',
  permissions: ['projects.read'] as string[],
};
```

```tsx
// src/pages/(app)/projects/index.tsx
import type { ColumnDef } from '@tanstack/react-table';
import { DataGrid, useDataGrid, DateCell } from '@/components/data-grid';
import { PageHeader } from '@/components/data-states';
import type { ProjectDto } from '@/lib/api/projects'; // hand-authored — see note below
import { useDocumentTitle } from '@/hooks/use-document-title';
import { meta } from './meta';

export default function ProjectsPage() {
  useDocumentTitle(meta.title);
  const grid = useDataGrid<ProjectDto>({ endpoint: '/projects', defaultSort: { id: 'createdAt', desc: true } });

  const columns: ColumnDef<ProjectDto>[] = [
    { id: 'name', accessorKey: 'name', header: 'Name', meta: { label: 'Name' } },
    {
      id: 'createdAt',
      header: 'Created',
      meta: { label: 'Created' },
      cell: ({ row }) => <DateCell value={row.original.createdAt} />,
    },
  ];

  return (
    <>
      <PageHeader title="Projects" />
      <DataGrid grid={grid} columns={columns} />
    </>
  );
}
```

> Columns are TanStack Table `ColumnDef<T>` — `id`/`accessorKey` for the field, `header` for the label, `cell` for custom rendering (reuse `DateCell`/`BadgeCell` from the same barrel), `meta.label` for the column-visibility menu. A column with no `accessorKey` should set `enableSorting: false`. `DataGrid` + `useDataGrid` come from the `@/components/data-grid` barrel; `PageHeader`/`EmptyState`/`ErrorState`/`LoadingSkeleton` from `@/components/data-states`.

> `(app)` and `(auth)` are **route groups** — parentheses organize files without adding a URL segment. `_`-prefixed files/dirs are ignored by the router (same convention as the backend `_Template`).

**The typed API client is hand-authored, not generated.** Add `src/lib/api/projects.ts` mirroring the existing modules (`comments.ts`, `tenancy.ts`, …) — declare the DTO interfaces and export a `projectsApi` object wrapping the shared fetch client:

```ts
// src/lib/api/projects.ts
import { api } from './client';

export interface ProjectDto { id: number; name: string; description: string | null; createdAt: string; }
export interface CreateProjectRequest { name: string; description?: string | null; }

export const projectsApi = {
  get: (id: number) => api.get<ProjectDto>(`/projects/${id}`),
  create: (body: CreateProjectRequest) => api.post<ProjectDto>('/projects', body),
};
```

(Routes themselves *do* regenerate automatically — the generouted Vite plugin runs on `npm run dev`. There is no `npm run codegen`.)

**Don't ship until the six-point bar is green** (loading skeleton, empty state, error state, mobile, dark mode, keyboard — see [CONVENTIONS.md](CONVENTIONS.md)). `<DataGrid>` gives you most of these for free; verify them.

---

## 3. Add a permission

1. Constant in the slice's `Permissions.cs` (see recipe 1).
2. `.RequirePermission("projects.archive")` on the endpoint.
3. Assign to a role at `/admin/roles`.

Wildcards work: a role granted `projects.*` satisfies every `projects.x`. The permission auto-appears in the admin catalog because any `*Permissions` class is reflection-discovered.

---

## 4. Build a list end-to-end (DataGrid)

The backend speaks **operator-suffix query syntax** and returns `PagedResult<T>`; the frontend `useDataGrid` builds that query string and `<DataGrid>` renders it.

**Backend** — declare a `QuerySpec<T>` allowlist (only listed fields are filterable/sortable/searchable — clients can't probe arbitrary columns), then `ToPagedResultAsync`:

```csharp
// A per-endpoint allowlist. .Allow() = filterable AND sortable; .FilterOnly() / .Searchable() / .DefaultSort() refine.
private static QuerySpec<Project> Spec() => new QuerySpec<Project>()
    .Allow("name", p => p.Name)
    .Allow("createdAt", p => p.CreatedAt)
    .Searchable(p => p.Name)
    .Searchable(p => p.Description)
    .DefaultSort("createdAt", descending: true);   // stable order when the request specifies none

private static async Task<IResult> List(PagedRequest request, AppDbContext db, CancellationToken ct)
{
    var paged = await db.Set<Project>().AsNoTracking()
        .ToPagedResultAsync(request, Spec(), p => p.ToDto(), ct);   // CountAsync + Skip/Take + map
    return Results.Ok(paged);
}
```

That endpoint now answers `GET /api/projects?page=1&pageSize=20&name=contains:foo&createdAt=gte:2026-01-01&sort=name:asc`. Supported operators: `eq` `ne` `gt` `gte` `lt` `lte` `contains` `startswith` `endswith` `in:a,b` `nin:a,b` `null` `notnull`. `sort=field:asc|desc` (comma-separated for multi-sort). `search=` runs across the `.Searchable(...)` fields. Unknown fields and unparseable operands are silently skipped rather than 500-ing.

**Frontend** — `useDataGrid({ endpoint })` owns paging/sort/search/filter state and fetches via TanStack Query; `<DataGrid>` owns selection and column visibility (see recipe 2 for the JSX).

> SQLite gotcha: date columns only sort/filter because `SqliteValueConverters` stores `DateTimeOffset` as ticks. Money sorts because it's stored as INTEGER cents (recipe 14).

---

## 5. Report an error (ProblemDetails)

**Never `throw new Exception(...)`.** Throw a `DomainException` subclass; the global handler renders RFC 7807 ProblemDetails with `status`, `title`, `detail`, `code`, and `traceId`:

```csharp
throw new NotFoundException(nameof(Project), id);          // 404  NOT_FOUND
throw new BadRequestException("Start date must precede end date."); // 400 BAD_REQUEST
throw new ConflictException();                             // 409  CONFLICT
throw new ForbiddenException();                            // 403  FORBIDDEN
throw new UnauthorizedException("Bad code", "INVALID_2FA"); // 401 with a typed code
throw new ValidationException(new Dictionary<string, string[]>
{
    ["email"] = ["Already in use."],
});                                                        // 400  VALIDATION_FAILED + field errors
```

The FE `<ErrorState>` reads `code` to branch and shows `traceId` in a fold-out. Field errors map back onto form fields.

---

## 6. Add a setting

Register a definition (anywhere that runs at startup — typically a slice's `ISettingsContributor`):

```csharp
SettingDefinitions.Register(
    key: "Projects.DefaultColor",
    type: typeof(string),
    scopes: [SettingScope.User, SettingScope.Tenant],
    defaultValue: "#6366f1",
    category: "Projects");
```

Read it anywhere via the injected `ISettingService`:

```csharp
var color = await _settings.GetAsync<string>("Projects.DefaultColor");
```

The admin settings UI and the profile UI re-render automatically from the registry — no UI code to write. Tenant-scoped settings key off `ITenantContext` for free.

---

## 7. Add a background job

**One-off** (enqueue from a handler):

```csharp
_jobs.Enqueue(() => ProjectReports.GenerateAsync(projectId, CancellationToken.None));
```

**Recurring** — `[RecurringJob("cron")]` on a `static` method; it's auto-scheduled at startup:

```csharp
public static class ProjectMaintenance
{
    [RecurringJob("0 3 * * *", Id = "projects:archive-stale")]
    public static async Task ArchiveStaleAsync()
    {
        using var scope = JobServices.Provider.CreateScope();   // DI inside a static job
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // ...
    }
}
```

Watch jobs run at `/hangfire` (dev only).

---

## 8. Make a slice findable in ⌘K (search provider)

Implement `ISearchProvider`, **scope to what the user may see**, and register via `IServiceRegistrar`:

```csharp
using NetForge.Server.Platform.Search;

internal sealed class ProjectSearchProvider(AppDbContext db) : ISearchProvider
{
    public const string ProviderKey = "Projects";
    public string Key => ProviderKey;          // identity for the admin toggle (Search.Projects)
    public string Category => "Projects";

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchContext ctx, CancellationToken ct)
    {
        if (!ctx.Can(ProjectPermissions.Read)) return [];   // permission-gate the lane
        var term = ctx.Query.ToLowerInvariant();            // case-insensitive (lower() both sides)
        return await db.Set<Project>().AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(term))
            .Take(5)
            .Select(p => new SearchHit(p.Name, p.Description, $"/projects/{p.Id}", Icon: "folder"))
            .ToListAsync(ct);
    }
}

public sealed class ProjectServiceRegistrar : IServiceRegistrar
{
    public void Register(IServiceCollection services) =>
        services.AddScoped<ISearchProvider, ProjectSearchProvider>();
}

// Admin on/off toggle for the lane (appears under the "Search" category in /admin/settings).
public sealed class ProjectSearchSettings : ISettingsContributor
{
    public void Register() =>
        SettingDefinitions.Register(
            SearchProviderSettings.EnabledKey(ProjectSearchProvider.ProviderKey), typeof(bool),
            [SettingScope.App], true, SearchProviderSettings.Category);
}
```

`ISearchService` fans out to every provider in parallel and merges the lanes behind the global ⌘K palette, skipping any whose `Search.{Key}` toggle an admin turned off.

---

## 9. Emit a webhook event

Declare the event name on a `*WebhookEvents` class (reflection-discovered into the catalog, like `*Permissions`):

```csharp
public static class ProjectWebhookEvents
{
    [Description("A project was created")]
    public const string Created = "project.created";
}
```

Publish it from your handler:

```csharp
await _eventBus.PublishAsync(ProjectWebhookEvents.Created, project.ToDto());
```

The dispatcher fans it out to matching active subscriptions and queues a signed (HMAC-SHA256) delivery per match, retried off the request path by Hangfire (5s/30s/5m/1h/6h backoff). Tenant admins subscribe + watch the delivery log at `/admin/webhooks`. You declare + publish; everything else is handled.

---

## 10. Add a dashboard widget

Export a `WidgetDefinition` from `src/widgets/widgets/`:

```tsx
// src/widgets/widgets/project-count.tsx
import { FolderKanban } from 'lucide-react';
import type { WidgetDefinition } from '../types';

export const projectCountWidget: WidgetDefinition = {
  type: 'project-count',
  title: 'Project count',              // the picker's name for the type
  description: 'How many active projects',
  icon: FolderKanban,
  titleOf: (config) => LABELS[String(config.metric)],  // the header for *this* instance
  defaultSize: { w: 3, h: 2, minW: 2, minH: 2 },
  narrowSpan: 'half',                  // pairs two-up below 996px; 'full' (default) otherwise
  permission: 'projects.read',         // omit = available to everyone
  defaultConfig: {},
  Component: ({ config }) => { /* render */ return null; },
};
```

Register it in `src/widgets/registry.ts` (add the import + push it onto the `ALL` array). It then appears in the "Add widget" picker and renders by its `type`. Data widgets bind to the `/api/dashboard` sources.

Three board-level properties decide how it reads next to everything else:

- **`titleOf` / `iconOf` / `accentOf`** — a configurable widget's header should name and colour what
  *this* instance shows, not its type. Four tiles headed "Stat" behind four identical glyphs tell the
  reader nothing. The body then carries neither label nor icon: on a tile this small, saying either
  twice costs a whole row (and it's the row that truncates first).
- **`narrowSpan`** — `'half'` if the whole content is one figure, so they pack four-across below 996px
  of grid and two-across below 700px. Everything else takes half the row at four columns and the whole
  row at two; a chart at ~170px is unreadable. Declare it rather than inferring from `defaultSize`:
  desktop width says how much room a widget was *given*, not how little it can live in.
- **Surface tier** — add a single-number tile to `LEAD_WIDGETS` (in `widget-card.tsx`, and its twin in
  the Angular `widget-host.ts`) or it renders as a quiet panel instead of a raised headline tile.

Bind a trend line to the metric's *own* series, and plot the one its number claims: a running total gets
the cumulative curve (`runningTotal(daily, value)` — monotonic, so it reads as growth); a metric whose
label is already a window ("this week") gets a rolling 7-day sum (`rollingSum(daily, 7)`, which also
rescues a series too coarse to draw — 0/1/2/3 is a zigzag between four values); one about a single day
gets the daily counts. A live count with no history plots nothing rather than borrowing a series from
elsewhere, which is what makes every tile on a board look identical.

**Keep the board hole-free.** Author every row to fill all twelve columns and every run of compact tiles
to a multiple of four, so the authored board *and* both narrow tiers pack cleanly. The packer closes
what's left over — it takes the next widget that fits a gap rather than stranding it, then grows
whatever column still ends short — but a hole showing up means the authored rows drifted.

---

## 11. Soft delete + restore

Add the `ISoftDeletable` marker to the entity:

```csharp
public class Project : ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    // ...
}
```

A global `!IsDeleted` query filter is applied automatically. To reach deleted rows (e.g. a "Deleted" facet or a restore endpoint), use `IgnoreQueryFilters()`:

```csharp
var deleted = await db.Set<Project>().IgnoreQueryFilters()
    .Where(p => p.IsDeleted).ToListAsync(ct);
```

> ⚠️ Don't combine `ISoftDeletable` and `ITenantScoped` on one entity — the second `HasQueryFilter` replaces the first. Write a single combined filter if you need both.

---

## 12. Optimistic concurrency

Give the entity a `Guid RowVersion` concurrency token:

```csharp
public Guid RowVersion { get; set; }
// EfConfig:
b.Property(x => x.RowVersion).IsConcurrencyToken();
```

On update, set the original value from the client's token and translate the EF exception to a 409:

```csharp
db.Entry(item).Property(x => x.RowVersion).OriginalValue = req.RowVersion;
item.RowVersion = Guid.NewGuid();
try { await db.SaveChangesAsync(ct); }
catch (DbUpdateConcurrencyException) { throw new ConflictException("This record changed since you loaded it."); }
```

---

## 13. Tenant-scope an entity

Add the `ITenantScoped` marker:

```csharp
public class Project : ITenantScoped
{
    public string TenantId { get; set; } = default!;
    // ...
}
```

A global `WHERE TenantId = current` filter applies automatically; the tenant comes from `ITenantContext`, resolved per request by `TenantResolutionMiddleware`. Read the active tenant in a handler with `ITenantContext.TenantId` (returns the seeded `"default"` constant in single-tenant mode and outside a request — jobs/seeders). The management UI, switcher, invitations, and resolution are already wired — adding the marker is the whole job. For per-tenant role assignment use `ITenantRoleService` (**never** write `AspNetUserRoles` directly — the claims factory only reads `TenantUserRole`).

---

## 14. Store money

Use `decimal` with the cents converter so values sort and range-filter in SQLite (which can't order over a real/decimal text column reliably):

```csharp
// EfConfig
b.Property(x => x.Price).HasConversion(SqliteValueConverters.DecimalToCents);
```

It's stored as INTEGER cents. Sum aggregates in memory as `decimal` (the dashboard does this).

---

## 15. Export & import (CSV / Excel / PDF)

**Export** — `TabularExport` turns any row set into CSV/Excel/PDF. Wire a `/export` endpoint and point the DataGrid export menu at it:

```csharp
group.MapGet("/export", (AppDbContext db, string format, CancellationToken ct) =>
    TabularExport.Create(rows, format, columns));
```

**Import** — `TabularImport.ReadRows(stream, fileName)` parses an uploaded CSV/XLSX into header-keyed rows. Validate per-row, dedup on a natural key, and return an `ImportResult` with per-row errors:

```csharp
var rows = TabularImport.ReadRows(file.OpenReadStream(), file.FileName);
foreach (var (row, i) in rows.Select((r, i) => (r, i)))
{
    if (string.IsNullOrWhiteSpace(row["Name"])) { result.AddError(i, "Name is required."); continue; }
    // ...upsert, dedup by SKU/email
}
```

---

## 16. Add an activity timeline + comments to a detail page

Every entity audited through the EF interceptor has a history at `(entityType, entityId)`. Drop both components onto a detail page — each owns its loading/empty/error states:

```tsx
<AuditTimeline entityType="Project" entityId={String(project.id)} />
<Comments entityType="Project" entityId={String(project.id)} />
```

`<AuditTimeline>` is the read-only activity feed; `<Comments>` adds discussion with `@mention` → notification and an autocomplete composer (deletes are author-or-`comments.moderate`). Nothing to wire on the backend — `/api/comments` and the audit query API already exist.

---

## 17. Add a language

1. Drop `src/locales/{lang}.json` (FE) and `Resources/{lang}.json` (BE).
2. In `src/i18n.config.ts`: import the JSON into `resources` and add a `LANGUAGES` entry `{ code, name (autonym), dir }`.

`supportedLngs` is derived from `LANGUAGES`. Set `dir: 'rtl'` for RTL scripts — the shell mirrors automatically via logical properties, so there's no separate RTL list to maintain. (This is why the codebase bans `ml-/mr-/pl-/pr-` in favour of `ms-/me-/ps-/pe-`.)

---

## 18. Add an OAuth provider

1. `AddXxx()` in the auth setup.
2. An `appsettings.json` config block with the client id/secret.

The login-page button auto-hides when its config is missing, so partially-configured environments stay clean. Google, Microsoft, and GitHub are already wired behind config.

---

## 19. Add a health check

Implement `IHealthCheck`, then register it in `Platform/Health/HealthSetup.cs`:

```csharp
services.AddHealthChecks()
    .AddCheck<ProjectQueueHealthCheck>("project-queue", tags: [HealthSetup.Ready]);
```

Tagged `ready` checks gate `/health/ready` and surface as a card on the `/admin/health` dashboard (and in the permission-gated `GET /api/health`). `/health/live` stays dependency-free for liveness probes.

---

## 20. Rate-limit or version a slice

**Rate limit** — the whole `/api/*` surface already has a generous global sliding window. Add the strict per-IP credential throttle to a group:

```csharp
group.RequireRateLimiting(RateLimitSetup.Auth);   // 10/min per IP — for credential endpoints
group.RequireRateLimiting(RateLimitSetup.Api);    // opt-in per-caller window for an expensive group
```

Define a new named policy in `RateLimitSetup` for a different shape — rejections already render as RFC 7807 ProblemDetails (`RATE_LIMITED` + `Retry-After`).

**Version** — the surface is unversioned-but-ready (default v1.0, `AssumeDefaultVersionWhenUnspecified`). Opt a group in explicitly:

```csharp
app.NewVersionedApi("Projects").MapGroup("/api/projects").HasApiVersion(ApiVersioningSetup.V1);
```

---

## 21. Test a slice

Copy the `_Template` test folders (they mirror the slice copy recipe):

**Unit** (`tests/Tests.Unit/Features/_Template/`) — pure validators + mappers. Runs as-is; rename and point at your types:

```csharp
[Fact]
public void Validator_requires_a_name()
{
    var v = new CreateProjectValidator();
    v.Validate(new CreateProjectRequest("Widget", null)).IsValid.ShouldBeTrue();
    v.Validate(new CreateProjectRequest("", null)).IsValid.ShouldBeFalse();
}
```

**Integration** (`tests/Tests.Integration/Features/_Template/`) — a `Skip`ped `WebApplicationFactory` endpoint test. Rename `Template` → `Project`, point at your route + permission constants, **delete the `Skip`**:

```csharp
[Collection(IntegrationCollection.Name)]
public sealed class ProjectsCrudTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Create_then_get_round_trips()
    {
        var client = factory.CreateAuthenticatedClient(permissions: [ProjectPermissions.Read, ProjectPermissions.Create]);
        var create = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("From a test", null));
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
```

Stack: **xUnit v3 + Shouldly + NSubstitute**. Integration tests share a `CustomWebApplicationFactory` (throwaway temp-file SQLite, header-driven `TestAuthHandler`, seeded once via the production `SalesSeeder`) over a collection fixture. `factory.CreateAuthenticatedClient(permissions: […])` mints exactly the permission claims a scenario needs.

> The `_Template` integration test is `Skip`ped *by design* — `_Template` is unregistered scaffolding (no route, no table), so the call would 404. It's a copy-source starting point, not a temporary skip.

---

## 22. Announce a release / add an onboarding step

**Changelog** — prepend an entry to `src/lib/changelog.ts` and bump `CURRENT_VERSION`. The top-bar "What's new" indicator lights an unread dot for everyone until they open the latest release at `/changelog`.

**Onboarding tour** — mark the target element with `data-tour="projects-nav"`, then add a step in `src/lib/onboarding/tour.ts`. Steps whose target isn't visible are dropped automatically, so it's safe to reference conditionally-rendered UI.

**PWA** — no per-feature wiring. The service worker precaches new assets on the next production build automatically (it's off in dev so HMR never fights a cache).
