# NetForge — Conventions Cheat-Sheet

The one-screen reference. For *what to do* see [../CLAUDE.md](../CLAUDE.md); for *how* see [RECIPES.md](RECIPES.md).

**Edition note:** describes the full NetForge platform; a trimmed/Community scaffold may not include every subsystem. The full online docs — which mark each feature Community or Pro — are at <https://docs.netforge.ebenmonney.com>.

---

## Naming

| Thing | Convention | Example |
|---|---|---|
| C# types / members | `PascalCase` | `ProductEndpoints`, `ToDto()` |
| C# private fields | `_camelCase` | `_settings`, `_jobs` |
| C# DTOs | `record` | `record ProductDto(int Id, …)` |
| TS vars / functions | `camelCase` | `useDataGrid`, `pagedResult` |
| TS components / types | `PascalCase` | `DataGrid`, `WidgetDefinition` |
| TS file names | `kebab-case` | `use-data-grid.ts` |
| React component files | `PascalCase.tsx` | `DataGrid.tsx` |
| Folders (everywhere) | lowercase `kebab-case` | `src/pages/(app)/sales/` |
| Permissions | `feature.action`, lowercase, dotted | `products.read`, `products.*` |
| i18n keys | `feature.subkey.subsubkey` | `sales.products.title` |
| Settings | `Feature.SettingName` | `Sales.TaxRate` |

---

## Backend slice anatomy (`Features/{Domain}/`)

Copy `Features/_Template/` — never hand-roll. Each slice is six files:

| File | Holds |
|---|---|
| `Endpoints.cs` | `IFeatureEndpoints` impl — `MapGroup` + per-action `.RequirePermission(...)` + filters |
| `Models.cs` | entity + DTO `record`s + request `record`s |
| `Validators.cs` | `internal sealed` `AbstractValidator<TRequest>` per request type |
| `Mappings.cs` | `internal static` `ToDto()` extension methods (no AutoMapper) |
| `Permissions.cs` | `static class {Domain}Permissions` — one `const string` per action |
| `EfConfig.cs` | `IEntityTypeConfiguration<T>` |

Auto-registered by reflection (`MapAllFeatures()`). **Never touch `Program.cs`.** Underscore-prefixed (`_Template`) = excluded from registration *and* EF model building.

## Frontend route anatomy (`src/pages/{path}/`)

The path tree **is** the URL tree (generouted; `src/pages` is the required base).

| File | Role |
|---|---|
| `index.tsx` | the screen (default export); optional same-file `Pending` / `Catch` exports = loading / error |
| `_layout.tsx` | wraps a subtree |
| `meta.ts` | `{ title, permissions }` |
| `(group)/` | organizes without affecting the URL |
| `_anything` | **ignored by the router** (same convention as backend `_Template`) |

Routes regenerate automatically (generouted Vite plugin on `npm run dev`). The typed API client is **hand-authored** per slice at `src/lib/api/{slice}.ts` (interfaces + a `{slice}Api` object over the shared `api` client) — there is no `npm run codegen`.

---

## The four endpoint filters (`MapGroup(...).AddEndpointFilter<X>()`)

| Filter | Does | Apply |
|---|---|---|
| `ValidationFilter` | runs `IValidator<TRequest>` → 400 ProblemDetails | per group |
| `AuditFilter` | audit log on successful writes | per group |
| `PerformanceFilter` | warns > 500 ms; adds `X-Response-Time` | per group |
| `TransactionFilter` | wraps handler in a DB transaction | **per write handler** (reads skip it) |

---

## The four platform contracts (don't reinvent)

- **Errors** = RFC 7807 ProblemDetails, always. Throw a `DomainException` subclass — **never `throw new Exception(...)`**.
- **Lists** = `PagedRequest` / `PagedResult<T>` + operator-suffix query (`?price=gte:10&status=in:a,b&sort=name:asc`). FE: `<DataGrid>` + `useDataGrid()`.
- **Cross-cutting** = the `IEndpointFilter` pipeline (above), never per-handler wiring.
- **Auth** = cookie scheme default (1-min `ValidationInterval`); Bearer behind a config flag (in-memory access token + httpOnly refresh cookie — **never `localStorage`**).

---

## Reuse these — don't build your own

| Need | Use |
|---|---|
| A list screen | `useDataGrid({ endpoint })` + `<DataGrid>` (BE returns `PagedResult<T>`; grid state auto-persists to the URL) |
| Multi-select column filter | `<FacetFilter>` from `@/components/data-grid` (emits the `in:` operator) |
| A card/grid view for a list | pass `renderCard={(row) => …}` to `<DataGrid>` (adds a Table⇄Cards toggle, choice saved per `viewKey`) |
| CSV / Excel / PDF export | `TabularExport` |
| CSV / XLSX import | `TabularImport.ReadRows(stream, fileName)` |
| Make a slice findable in ⌘K | implement `ISearchProvider` (set `Key`, register via `IServiceRegistrar`, scope with `SearchContext.Can(...)`) + a `Search.{Key}` toggle so admins can disable the lane |
| Audit trail | `IAuditService` / the EF audit interceptor; read back with `<AuditTimeline entityType entityId>` |
| Discussion + `@mentions` | `<Comments entityType entityId>` + `/api/comments` |
| Outgoing webhook event | declare on a `*WebhookEvents` class → `IEventBus.PublishAsync("entity.event", payload)` |
| Background job | `_jobs.Enqueue(...)` (one-off) or `[RecurringJob("cron", Id=…)]` on a `static` method |
| DI bindings for a slice | `IServiceRegistrar` (reflection-discovered) |
| Soft delete | add `ISoftDeletable` → global `!IsDeleted` filter; `IgnoreQueryFilters()` to reach deleted |
| Tenant scoping | add `ITenantScoped` → global `WHERE TenantId = current`; read via `ITenantContext.TenantId` |
| Per-tenant role assignment | `ITenantRoleService` — **never** write `AspNetUserRoles` directly |
| Optimistic concurrency | `Guid RowVersion` `.IsConcurrencyToken()` → catch `DbUpdateConcurrencyException` → `ConflictException` |
| Money | `decimal` + `SqliteValueConverters.DecimalToCents` (stores INTEGER cents so it sorts/filters in SQLite) |
| A setting | `SettingDefinitions.Register("Feature.Key", …)` → `_settings.GetAsync<T>(...)` |
| A dashboard widget | export a `WidgetDefinition` from `src/widgets/widgets/*` → add to `src/widgets/registry.ts` |
| A health check | `.AddCheck<T>("name", tags:[HealthSetup.Ready])` in `Platform/Health/HealthSetup.cs` |
| Strict credential throttle | `.RequireRateLimiting(RateLimitSetup.Auth)`; opt-in per-caller window: `RateLimitSetup.Api` |
| Version a slice | `app.NewVersionedApi("Name").MapGroup(...).HasApiVersion(ApiVersioningSetup.V1)` |

> ⚠️ `ISoftDeletable` + `ITenantScoped` on one entity: the second `HasQueryFilter` **replaces** the first. Combine the predicates by hand if you need both.
> ⚠️ SQLite can't translate `DateTimeOffset` ordering/comparison — `SqliteValueConverters` stores it as ticks so date columns sort in dev.

---

## UI Definition of Done (non-negotiable — six points)

A feature ships only when **all six** are true (else cut the feature, don't lower the bar):

1. **Loading** — skeleton matching content shape (spinners are for in-button submit only).
2. **Empty** — icon + headline + helper sentence + primary action (never bare "No results found.").
3. **Error** — plain language + Retry + traceId in a fold-out (never raw JSON).
4. **Mobile** — verified on a phone viewport (sidebar → drawer, table → cards, ≥ 44 px touch targets).
5. **Dark mode** — parity verified; intentional in both themes.
6. **Keyboard** — tab order, focus rings, Escape closes, Enter submits.

Reuse: `<EmptyState>`, `<ErrorState>`, `<LoadingSkeleton>`, `<PageHeader>`.

---

## Tailwind v4 + dark mode (gotchas)

- Colors live as CSS vars in `:root` / `.dark`, wrapped `hsl(...)`; mapped to utilities via `@theme inline`. No `tailwind.config.ts`.
- `@layer base` references vars **unwrapped** (`var(--background)`, not `hsl(var(--background))`).
- Use semantic tokens (`bg-primary`, `bg-destructive`) — **not** `dark:` variants for theme colors.
- Logical properties only (`ms-`/`me-`/`ps-`/`pe-`) so RTL mirrors automatically.

---

## Commits

```
<type>(<scope>): <imperative summary ~60 chars>

<optional body — WHY, not WHAT>
```

- **Types:** `feat` `fix` `chore` `docs` `refactor` `test` `perf` `ci` `build` `style`.
- Each commit **builds**. One logical change per commit.
- Stage specific files (`git add path`) — **never** `git add .` / `-A`.
- Push only when asked; never force-push without an explicit request.
- Doc drift (CLAUDE.md / this sheet) goes in the **same commit** as the change that caused it.

---

## Don't do this

❌ MediatR/CQRS · ❌ AutoMapper · ❌ `Repository<T>` over EF · ❌ Server Components/SSR · ❌ `useEffect` for fetching · ❌ tokens in `localStorage` · ❌ `throw new Exception(...)` · ❌ `ml-/mr-/pl-/pr-` · ❌ editing `Program.cs` to add a feature · ❌ a new abstraction without 3 real call sites · ❌ comments explaining *what* (only *why*) · ❌ spinner-only loading / "no results found" / raw error JSON · ❌ "I'll fix dark/mobile later" · ❌ a 7th feature while the existing 6 miss the bar.
