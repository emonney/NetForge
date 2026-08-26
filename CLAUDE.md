# Project guide

The agent/contributor guide for this app — short by design; read it before making changes. This project was scaffolded from [NetForge](https://netforge.ebenmonney.com).

## What this is

An ASP.NET Core 10 + React 19 line-of-business app: a typed minimal-API backend and a file-system-routed
React SPA, with auth, role-based permissions, an admin console, settings, and theming already wired.

## Architecture in 5 bullets

1. **Backend = vertical slices** under `Server/Features/{Domain}/` — each slice owns its `Endpoints.cs`,
   `Models.cs`, `Validators.cs`, `EfConfig.cs`, `Mappings.cs`, `Permissions.cs`. Slices self-register via an
   `IFeatureEndpoints` marker — **don't edit `Program.cs` to add a feature.**
2. **Frontend = file-system routes** under `client/src/pages/{path}/` — the screen is `index.tsx`; `_layout.tsx`
   wraps a subtree; `meta.ts` holds `{ title, permissions }`. Any `_`-prefixed file/dir is ignored by the router.
3. **Errors = RFC 7807 ProblemDetails, always.** Never `throw new Exception(...)` — throw a `DomainException`
   subclass and the global handler maps it.
4. **Lists = `PagedRequest` / `PagedResult<T>`** with operator-suffix query syntax
   (`?price=gte:10&status=in:a,b&sort=name:asc`); on the FE use `useDataGrid()` + `<DataGrid>`.
5. **Cross-cutting concerns = `IEndpointFilter` pipeline** (validation, audit, transaction, performance) —
   applied per slice, not re-implemented per handler.

## Add a feature

1. **BE:** copy `Server/Features/_Template` → `Features/{Domain}`, rename `Template` → `{Domain}`, then
   `dotnet ef migrations add Add{Domain}`. It auto-registers.
2. **FE:** copy `client/src/pages/_template` → `pages/{domain}`, edit `meta.ts`, add a typed API module under
   `client/src/lib/api/{domain}.ts`.
3. **Permissions:** add constants in `{Domain}/Permissions.cs`, gate endpoints with `.RequirePermission(...)`,
   assign via `/admin/roles`.

## Extend, don't duplicate

Reuse the platform you were scaffolded with. Add widgets to the **existing** dashboard via the widget registry
(`client/src/widgets/`) — never a second dashboard or a parallel nav entry. Extend the existing nav, settings,
data-grid, auth, and audit instead of building parallel versions. Turn on capabilities like multi-tenancy by
**marking** an entity (`ITenantScoped`), not by re-implementing them.

## Conventions

- **C#:** `PascalCase` types/members, `_camelCase` private fields, records for DTOs, manual mapping (no AutoMapper).
- **TS:** `camelCase` vars/functions, `PascalCase` components; file names `kebab-case` except `PascalCase.tsx` components.
- **Permissions:** `feature.action` (lowercase, dot-separated; wildcards `feature.*`).
- **Theming:** use logical CSS properties (`ms-`/`me-`/`ps-`/`pe-`) not `ml-`/`mr-` so RTL works.
- **Comments** explain *why*, not *what*.

## The UI/UX bar (non-negotiable)

Every feature ships with: a **loading state** (skeleton, not a spinner), an **empty state** (icon + headline +
action), an **error state** (plain language + retry), a **responsive mobile layout** (drawer nav, card lists,
≥44px targets), **dark-mode parity** (theme tokens, not hard-coded colors), and **keyboard access** (visible
focus, Escape/Enter). Build these into the components; if you can't cover all six, cut scope rather than ship thin.

## Don't

- ❌ MediatR/CQRS ceremony, AutoMapper, or a `Repository<T>` over EF — use `DbContext` directly.
- ❌ `useEffect` for data fetching — use TanStack Query (React `useQuery`, Angular `injectQuery`).
  Angular: don't reach for `resource`/`httpResource` either — no shared cache, no invalidation.
- ❌ Inlining a cache key — use the slice's `*Keys` factory, so a detail key can't drift from the one
  its page reads (a mismatch fails silently).
- ❌ Driving a skeleton from `isFetching` — that's for a progress bar over content already on screen.
  Skeletons come from `isLoading`, wrapped in `useDelayedFlag()` / `delayedFlag()`.
- ❌ `localStorage` for auth tokens — cookies handle it.
- ❌ Editing `Program.cs` to wire a feature — discovery is reflection-based.

## See also

Deeper reference, all in this repo:

- **[docs/USER_GUIDE.md](docs/USER_GUIDE.md)** — feature-by-feature guide, plus how to run and configure.
- **[docs/RECIPES.md](docs/RECIPES.md)** — copy-pasteable how-tos (add a feature, list, setting, job, …).
- **[docs/CONVENTIONS.md](docs/CONVENTIONS.md)** — the one-screen conventions cheat-sheet.
- **[docs.netforge.ebenmonney.com](https://docs.netforge.ebenmonney.com)** — the full online documentation.
