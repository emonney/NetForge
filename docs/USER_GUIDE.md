# NetForge — User Guide

A practical, feature-by-feature guide to what NetForge gives you and how to run it. For copy-pasteable how-tos see [RECIPES.md](RECIPES.md); for the one-screen reference see [CONVENTIONS.md](CONVENTIONS.md); for AI-agent guidance see [../CLAUDE.md](../CLAUDE.md).

**Edition note:** this guide covers the full NetForge platform. Depending on the edition and options this project was scaffolded with, some features below may not be present — the full online docs — which mark each feature Community or Pro — are at <https://docs.netforge.ebenmonney.com>.

---

## 1. What NetForge is

NetForge is an opinionated starter template for **ASP.NET Core 10 + React 19** line-of-business apps.
It compresses the first weeks of a typical SaaS / internal-tool build into a `git clone`: you get
authentication, authorization, and a platform layer (errors, validation, auditing, settings, caching,
email, jobs, blob storage, multi-tenancy) already wired and verified — plus a UI that looks like a
2026 product on first run.

It's also built to be **AI-extensible**: a consistent vertical-slice backend and file-system-routed
frontend mean an assistant can add a whole feature from one prompt without spelunking.

---

## 2. Quick start

### Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0** | `dotnet --version` should report 10.x |
| Node.js | **20+** (LTS) | for the Vite/React client |
| A dev HTTPS cert | — | `dotnet dev-certs https --trust` (first run only) |

No database to install — development uses a self-creating **SQLite** file.

### Run it (one command)

```bash
dotnet run --project NetForge.Server
```

That's it. The server project is configured with **SpaProxy**, so `dotnet run` automatically launches
the React dev server (`npm run dev`) for you. On first run it also:

- creates the SQLite database and applies all migrations, and
- seeds a ready-to-use admin account.

| Surface | URL |
|---|---|
| **App (use this)** | `https://localhost:3000` |
| API (backend) | `https://localhost:7000` |
| Interactive API docs (Scalar) | `https://localhost:7000/scalar` *(dev only)* |
| Background-jobs dashboard (Hangfire) | `https://localhost:7000/hangfire` *(dev only)* |

> First time only: if the browser warns about the certificate, run `dotnet dev-certs https --trust`
> and restart.

### Sign in

A pre-confirmed administrator is seeded in development:

| Email | Password |
|---|---|
| `admin@netforge.local` | `Admin123!$` |

> **Dev-only defaults.** These come from `appsettings.Development.json` (`Seed:Admin`). Production seeds
> an admin too, but only from an explicit `Seed:Admin:Email` / `Seed:Admin:Password` — the local defaults
> are never used on a non-Development deploy (see [§3.1](#31-all-appsettings-options)).

The admin holds the built-in **Admin** role, which grants the `*` (all-permissions) wildcard — so you
can immediately explore every screen, including **Administration** (`/admin`).

---

## 3. Configuration

All settings live in `NetForge.Server/appsettings.json`, with `appsettings.Development.json` overriding
for local runs and `appsettings.Production.json` (or **environment variables / user-secrets**) for
deploys. Anything secret — passwords, client secrets, SMTP keys — belongs in environment variables or
user-secrets in production, **never committed**.

### 3.1 All appsettings options

| Key | Default | What it does |
|---|---|---|
| `ConnectionStrings:Default` | SQLite `App_Data/netforge.db` | EF Core connection string. Must match `Database:Provider`. |
| `Database:Provider` | `sqlite` | Active EF Core provider: `sqlite`, `postgres`, or `sqlserver`. See [§3.2](#32-database-providers). |
| `App:ProductName` | `NetForge` | Branding shown in emails and the UI. |
| `App:ClientUrl` | (empty) | Absolute URL of the SPA, used to build email links. Empty = infer from the request origin (correct when SPA + API share an origin). |
| `App:BrandColor` | scaffold brand | Accent colour for transactional emails (any CSS colour); neutral fallback if unusable. |
| `Auth:Scheme` | `Cookie` | `Cookie` (default) or `Bearer`. See [§4.7](#47-cookie-vs-bearer-auth). |
| `Auth:RequireConfirmedEmail` | `true` | Require email confirmation before sign-in. |
| `Auth:OAuth:{Google,Microsoft,GitHub}:{ClientId,ClientSecret}` | (empty) | OAuth credentials per provider. A provider with blank credentials is **hidden** automatically. |
| `Seed:Admin:{Email,Password}` | dev: local defaults | Initial administrator, seeded idempotently on boot. **Required in production** — a known-default admin is never created on a non-Development deploy. Set via env vars / user-secrets. |
| `Seed:DemoData` | `false` | Seed the sample demo users + Sales data outside Development (e.g. a public demo). Always on in Development. |
| `Email:FromName` | (empty) | Display name on the From header (falls back to the bare address). |
| `Email:FromAddress` | (empty) | Sender address. Set this **and** `Email:Smtp:Host` to switch from console logging to real SMTP. |
| `Email:Smtp:Host` | (empty) | SMTP relay host (e.g. `smtp-relay.brevo.com`). |
| `Email:Smtp:Port` | `587` | SMTP port. |
| `Email:Smtp:Username` / `:Password` | (empty) | SMTP credentials (keep the password in env vars / user-secrets). |
| `Email:Smtp:UseStartTls` | `true` | STARTTLS on a submission port (587). `false` = implicit TLS (465). |
| `Tenancy:Mode` | `SingleTenant` | `MultiTenant` activates per-tenant resolution + the tenancy UI. |
| `ConnectionStrings:Hangfire` | SQLite `App_Data/hangfire.db` | Background-job store. |
| `Serilog:*` | Information | Logging levels (standard Serilog config). |
| `AllowedHosts` | `*` | Host filtering. |

### 3.2 Database providers

The model is **provider-agnostic** — NetForge runs on SQLite, PostgreSQL, or SQL Server. The provider is
chosen at scaffold time (`--database`, see [§3.5](#35-scaffolding-options)) and is otherwise a config
switch: set `Database:Provider` + a matching `ConnectionStrings:Default`.

| Provider | Best for | Notes |
|---|---|---|
| **SQLite** (default) | Local dev; small / single-instance production | Zero setup, in-process (fastest for reads). Ships **EF migrations**, applied on boot. Single writer — heavy concurrent writes serialize. |
| **PostgreSQL** | Production (recommended) | Scaffold ships `docker-compose.yml` → `docker compose up -d` is a local Postgres in one command. |
| **SQL Server** | Production on the Microsoft stack | Scaffold ships `docker-compose.yml`. |

**Migrations vs. `EnsureCreated`.** SQLite ships a flattened `InitialCreate` and applies migrations on
boot. Postgres / SQL Server scaffolds create their schema directly from the model with **`EnsureCreated`**
on first boot, so they run immediately — but without versioned migration history. The boot logic is
simply: SQLite → `Database.Migrate()`, server providers → `Database.EnsureCreated()`.

#### Moving a server DB to migrations

`EnsureCreated` is only the *starting* schema. When you want versioned, evolvable schema on Postgres /
SQL Server (the production-grade path), switch once — a fresh scaffold has no data, so it's clean:

1. **Stop the app** so nothing holds the database open.
2. **Drop the `EnsureCreated` database.** It has no `__EFMigrationsHistory` table, so a migration can't be
   applied on top of it. With the bundled docker DB: `docker compose down -v && docker compose up -d`.
3. **Delete the shipped `Data/Migrations` folder** — it holds the template's SQLite-shaped migration, and
   you'll regenerate one for your provider.
4. **Generate the initial migration** for your provider:
   ```bash
   dotnet ef migrations add InitialCreate --project YourApp.Server
   ```
5. **Apply it:** `dotnet ef database update --project YourApp.Server`.
6. **Auto-apply on boot (optional but recommended):** in `Program.cs`, change the database-init line from
   `db.Database.EnsureCreated();` to `db.Database.Migrate();`. From here every schema change is the normal
   `dotnet ef migrations add <Name>` → `dotnet ef database update`. Commit the `Data/Migrations` folder.

> The runtime already supports migrations on any provider — `EnsureCreated` is just the chosen *starting*
> schema for server DBs, so the scaffold builds and runs without shipping fragile per-provider migration
> sets. SQLite users skip all of this; migrations are there from the start.

**Switching an existing SQLite project to a server DB:** add the provider's EF package (`Npgsql.
EntityFrameworkCore.PostgreSQL` or `Microsoft.EntityFrameworkCore.SqlServer`), set `Database:Provider` +
the connection string, and regenerate migrations for the new provider.

#### Dev/prod split (SQLite locally, server DB in production)

Scaffolding with `--devSplit true` (or the configurator's *"Use SQLite for local dev"* checkbox) puts
SQLite in `appsettings.Development.json` while production uses the server DB. It lowers local friction,
but carries a real **parity risk**: SQLite stores dates and money differently (NetForge applies
SQLite-only value converters), and case-sensitivity, JSON querying and concurrency all differ — so you
develop and test against a *different* engine than you ship. **Prefer the bundled docker-compose** to
develop against the real engine; choose the split only if you accept that trade-off.

### 3.3 Email

By default (no `Email:Smtp:Host` / `Email:FromAddress`) NetForge uses a **console email sender**: every
message it would send (confirmation links, password resets, invitations) is written to the **server
log** — watch the `dotnet run` output to grab links during local testing.

Set `Email:FromAddress` **and** `Email:Smtp:Host` to send for real over SMTP (MailKit). Works with any
relay — **Brevo** (`smtp-relay.brevo.com:587`, STARTTLS), SendGrid, Mailgun, Gmail, or your own server.
Keep the SMTP password in environment variables / user-secrets. All emails share one branded, table-based
template (the accent comes from `App:BrandColor`).

### 3.4 Production checklist

- Set `Seed:Admin:Email` + `Seed:Admin:Password` (**required** — no admin is seeded otherwise).
- Set `Database:Provider` + `ConnectionStrings:Default` for your database.
- Configure `Email:*` so confirmation / reset emails actually send.
- Set `App:ClientUrl` if the SPA and API are on different origins.
- Provide OAuth credentials for any providers you want visible.

### 3.5 Scaffolding options

Three ways to create a project, all driving the same options:

- **CLI wizard** (easiest) — `dotnet tool install -g NetForge.Cli`, then `netforge new` walks you through edition, database, features, OAuth, and theme, resolves feature dependencies, and runs `dotnet new` for you. Scriptable too: `netforge new -n MyApp --tier basic --with dashboard,webhooks --yes` (or `--dry-run`).
- **Raw template** — the free Basic edition is `dotnet new install NetForge.Templates`, then `dotnet new netforge -n MyApp`. The full Pro template (`NetForge.Pro.Templates`, with the `--tier`/feature/database flags below) installs from the sponsor-gated feed (see the configurator).
- **Online configurator** — pick features visually at <https://netforge.ebenmonney.com> and download a ZIP.

The options map 1:1 across all three:

| CLI flag | Configurator | Values | Default | Effect |
|---|---|---|---|---|
| `-n, --name` | Project name | identifier | `NetForge` | Names the solution, namespaces, and folders. |
| `--tier` | Edition | `pro` \| `basic` | `pro` | `basic` strips the premium subsystems (multi-tenancy, webhooks, audit, dashboard, search, notifications, jobs, 2FA/OAuth/sessions, file uploads, export, PWA/tour/changelog, demo) for the free MIT edition. |
| `--database` | Database | `sqlite` \| `postgres` \| `sqlserver` | `sqlite` | Provider the scaffold is wired for ([§3.2](#32-database-providers)). Server DBs ship a docker-compose. |
| `--devSplit` | Use SQLite for local dev | `true` \| `false` | `false` | (Server DB only) SQLite locally, server DB in production — parity caveat above. |
| `--IncludeDemo` | Demo (Sales) domain | `true` \| `false` | `true` (Pro) | The sample Sales vertical + its tests. Pro only. |
| `--brandColor` | Accent colour | CSS colour | (default theme) | Starting brand accent, baked into the scaffold. |
| `--brandTheme` | Theme | `ocean`/`forest`/`sunset`/`zinc`/… | (default theme) | Starting curated theme. |
| `--opt<Feature>` | per-feature toggles | `default`/`on`/`off` | `default` | Force an individual subsystem on/off regardless of tier (e.g. `--optWebhooks off`). |
| `--Google` / `--Microsoft` / `--GitHub` | OAuth providers | `true`/`false` | `true` | Include that OAuth provider's wiring (still config-gated at runtime). |

Everything is tunable later — the database via `Database:Provider`, the theme/accent via
`/admin/appearance`, permissions via `/admin/roles`.

---

## 4. Authentication

A complete, production-shaped auth system with beautiful split-panel screens (brand showcase + form),
verified in light, dark, and mobile.

### 4.1 Register & confirm email
`/register` creates an account and sends a confirmation link (in dev: to the server log). Until the
address is confirmed, sign-in is blocked (when `RequireConfirmedEmail` is on). A "resend confirmation"
path is included. Account enumeration is avoided — responses don't reveal whether an email exists.

New sign-ups are granted a **default role** so their first sign-in lands on a usable app instead of an
empty shell. The role is the `Account.DefaultRole` setting (in **Admin → Settings → Account**), which
defaults to the seeded **Member** role — a normal, editable role granted read-only access to the
product **catalog** (products + categories) out of the box, but not orders or customers (financial/PII
data isn't a default grant). Tune what every new user can do by editing Member's permissions in
**Admin → Roles**, point the setting at a different role, or blank it to grant nothing. (Self-service
registration itself can be turned off via `Account.AllowRegistration` for invite-only instances.)

### 4.2 Sign in / out
`/login` with email + password, optional **Remember me** (persistent vs session cookie). Failed
attempts count toward lockout (5 attempts → 15-minute lockout).

### 4.3 Forgot / reset / change password
`/forgot-password` emails a reset link; `/reset-password` completes it. Signed-in users change their
password from their **Profile**. Changing a password rotates the security stamp (see [§4.5](#45-per-device-sessions)).

### 4.4 Two-factor authentication (TOTP)
From **Profile → Two-factor**, enrol with any authenticator app (Google Authenticator, 1Password,
Authy, …) by scanning a QR code. Enrolment issues **one-time recovery codes**. At login, a 2FA user
completes a second step (`/two-factor`) with either a 6-digit code or a recovery code. You can
disable 2FA or regenerate recovery codes at any time.

### 4.5 Per-device sessions
Every sign-in records a session (device, browser/OS, IP, last-seen). **Profile → Active sessions**
lists them and lets you **sign out a single device** or **sign out all others**. Revocation is
near-instant: the cookie is re-validated on a 1-minute interval, so a killed session stops working
within ~1 minute — without disturbing your other devices.

### 4.6 OAuth sign-in & account linking
Google, Microsoft, and GitHub are supported and **config-gated**: a provider only appears (button on
the login page, entry in Profile) if its client id/secret are set. Signing in with a provider
auto-provisions a confirmed account; **Profile → Connected accounts** links/unlinks providers (with a
guard against unlinking your only way back in).

### 4.7 Cookie vs Bearer auth
The default is a hardened **cookie** scheme (HttpOnly, `SameSite=Strict`, `Secure`, API endpoints
return `401/403` rather than redirecting). A **Bearer** scheme is registered and activated by flipping
`Auth:Scheme=Bearer` — in which case the access token is held **in memory** by the client and the
refresh token is an HttpOnly cookie. Tokens are never placed in `localStorage`/`sessionStorage`.

---

## 5. Your account (self-service profile)

`/profile` is a complete self-service surface:

- **Profile** — display name and verified-email indicator.
- **Preferences** — language and timezone, saved to your account so they follow you across devices
  (language also switches the UI immediately).
- **Password** — change password inline.
- **Two-factor** — enrol (QR), view recovery codes, disable, regenerate.
- **Active sessions** — see and revoke devices.
- **Connected accounts** — link/unlink OAuth providers.

---

## 6. Authorization (roles & permissions)

A first-class permission system with a **wildcard policy provider** — administered entirely from the
**Administration** area (`/admin`), reachable from the user menu when you have access.

### 6.1 How it works
- **Permissions** are dotted strings (`users.read`, `roles.update`) declared in code by each feature.
- A **catalog** is auto-aggregated at startup from those declarations — no central registry to edit.
- **Roles** bundle permissions. **Users** get roles. Effective permissions = the union across roles.
- **Wildcards** expand: a role granted `users.*` covers every `users.x`; `*` (the built-in **Admin**
  role) grants everything, including permissions added by features that don't exist yet.
- Permissions ride in the auth cookie and refresh on the 1-minute validation interval, so changing a
  role updates its members' access within ~1 minute (or immediately on their next sign-in).

### 6.2 Administration UI
| Page | What you can do |
|---|---|
| **Users** (`/admin/users`) | Search users; see roles and status (locked / pending email / 2FA); **create a user** (invite-first — email them a "set your password" link, or set a temporary password, optionally marking the email verified up front); **edit** name/email; assign roles; **mark a pending email as verified** or **resend the confirmation email**; **send a password-reset link** (revokes the user's active sessions; the user picks a new password — you never see it); **disable a user's 2FA** (if they're locked out of their authenticator); **view a user's activity** (their audit timeline); lock/unlock; delete. |
| **Roles** (`/admin/roles`) | Create/edit roles with a grouped permission picker (per-feature select-all); delete. The built-in **Admin** role is read-only. |
| **Permissions** (`/admin/permissions`) | Browse the full catalog, grouped by feature, with descriptions. Read-only — it's generated from the code. |

### 6.3 Safety rails
- The built-in **Admin** role can't be renamed or deleted.
- You can't lock, delete, or change the roles of **your own** account (no self-lockout).
- The UI only offers actions you're permitted to take — but the **API enforces every check
  regardless** (a hidden button is convenience, not a security boundary). Unauthenticated calls get
  `401`; authenticated-but-unauthorized calls get `403`.

### 6.4 Granting access (typical flow)
1. Open **Administration → Roles → New role**, name it, tick the permissions (or a whole feature
   group), and save.
2. Open **Administration → Users**, find the user, **Edit roles**, assign the new role.
3. They get the access on their next sign-in (or within ~1 minute).

---

## 7. Platform capabilities (available to developers)

These are wired and working under the hood — some power end-user screens already, others are developer APIs you can build on.

| Capability | Status | Notes |
|---|---|---|
| **Problem-details errors** | ✅ end-to-end | Every failure is RFC 7807 JSON with a `traceId`; the UI shows plain-language errors with the trace tucked into a fold-out, never raw JSON. |
| **Validation pipeline** | ✅ | FluentValidation runs automatically per request; field errors surface inline on forms. |
| **Auditing** | ✅ end-to-end | An EF interceptor records entity changes (sensitive fields redacted) off the request path; the **audit log UI** (`/admin/audit`) and per-entity history (`/audit/entity/{type}/{id}`) read it back. |
| **Global search** | ✅ end-to-end | `ISearchProvider`s fan out behind the ⌘K palette's **Find** lanes (orders, products, customers, users, settings, notifications), permission-scoped + case-insensitive. Each lane is admin-toggleable from **Settings → Search**; a slice becomes searchable by implementing one interface. |
| **Settings system** | ✅ API + UI | User→Tenant→App cascade with cached resolution. Features declare settings via `ISettingsContributor`; App settings render at **`/admin/settings`** (typed controls), user preferences on the profile. |
| **Caching** | ✅ | In-memory cache with tagged invalidation (Redis swaps in via config later). |
| **Blob storage** | ✅ end-to-end | Local-filesystem provider behind `IBlobStore`; `<FileUpload>` + Magick.NET image processing power avatar uploads (Azure/S3/R2 swap via config). |
| **Email** | ✅ (dev sender) | Console sender logs messages + links in dev; production SMTP is a drop-in swap. |
| **Background jobs** | ✅ | Hangfire is wired; one-off + recurring jobs supported. Dashboard at `/hangfire` (dev). |
| **Webhooks** | ✅ end-to-end | Outgoing webhooks with HMAC-SHA256 signing, Hangfire delivery + backoff retries, and a per-attempt delivery log; admins manage endpoints at `/admin/webhooks`. Slices emit events via `IEventBus.PublishAsync(...)`. |
| **Multi-tenancy** | ✅ end-to-end | Always-on plumbing, **single-tenant by default** (invisible); flip `Tenancy:Mode=MultiTenant` for per-tenant roles, switcher, branding, and invitations. |
| **Pagination & query** | ✅ end-to-end | `PagedRequest`/`PagedResult` with operator-suffix filters (`?price=gte:10&status=in:a,b&sort=name:asc`); the `<DataGrid>` + `useDataGrid` UI consumes it, with `<FacetFilter>` for multi-select column filters. |
| **API docs** | ✅ | Interactive Scalar UI at `/scalar` (dev). |

---

## 8. The app experience

### 8.1 Navigation & shell
A responsive app shell wraps every screen: a **collapsible sidebar** on desktop (toggle it to a slim
**icon rail** from the top bar — your choice is remembered; it becomes a **drawer** on mobile) and a top
bar. The navigation is **permission-aware** — you only see the entries you can open — and grouped
sections (e.g. **Administration**) **collapse** to tidy the rail, each remembering its open/closed state.
A **skip-to-content** link and keyboard focus styles support keyboard-only use.

### 8.2 Command palette & global search (⌘K)
Press **⌘K / Ctrl+K** (or click the prominent **search bar** centred in the top bar) to open the command
palette. Start typing and it **searches your data** — matching **orders, products, customers, users,
settings, and notifications** appear in grouped **Find** lanes above the usual **Navigate** (jump to any
screen) and **Actions** (profile, theme) lanes — all from the keyboard. Results are **permission-scoped**:
you only ever see what you're allowed to (e.g. users show only to admins). Search is case-insensitive, and
selecting a result deep-links straight to it.

**Configurable:** which record types appear is controlled per provider from **Settings → Search** — an
admin can switch any lane (orders, products, customers, …) on or off to match how the team works.

**For developers:** any slice becomes searchable by implementing **`ISearchProvider`** and registering
it (via `IServiceRegistrar`); the backend fans queries out to every provider in parallel and merges the
ranked lanes. Providers filter results to the current user with `SearchContext.Can(permission)`, and each
gets an admin on/off toggle (`Search.{Key}`) for free.

### 8.3 Theming
**Light / dark / system**, toggled from the top bar; every screen is verified for parity in both
themes, and `color-scheme` keeps native controls (selects, scrollbars) on-theme. Brand colors are CSS
variables, so re-skinning is a token change.

### 8.4 Languages & RTL
The UI ships in **six languages** — English, Spanish, French, German, Arabic, and Chinese — switchable
from the top bar or your profile preferences. **Arabic mirrors the entire layout right-to-left**
automatically (logical CSS properties throughout). Your language is saved to your account and follows
you across devices. Adding a language is one JSON file plus one entry in `i18n.config.ts`.

### 8.5 Notifications & real-time
A top-bar **bell** shows your notifications with a live **unread badge**. Everything updates in
**real time over SignalR** — a new notification (or marking one read on another device) appears
instantly, no refresh, and stays in sync across every device you're signed in on. New arrivals also
raise a toast; when the tab is in the background you can opt in to **OS-level browser notifications**.

The bell's dropdown lists your most recent items with **"Mark all read"** and **"See all."** The full
**`/notifications`** page filters by **All / Unread**, pages through your history, and lets you mark
items read/unread or dismiss them (with designed loading, empty, and error states throughout).

**Per-user preferences** live on your profile under **Notifications**: toggle **email copies** of each
notification (off the request path) and **browser notifications** (which asks the browser for
permission the first time). Old notifications are cleaned up automatically after 90 days.

**For developers:** one call — `INotificationService.NotifyAsync(userId, new NotificationPayload(...))`
— persists the notification, pushes it over the hub, and (per the user's setting) queues the email. In
Development there's a **"Send test"** button on the `/notifications` page to fire a sample to yourself.

### 8.6 Data tables, forms & files
Every list in the app is a **DataGrid**: sortable column headers, instant (case-insensitive) search,
per-column show/hide, **multi-select filters** (pick several values at once), page-size + pagination,
**row selection with bulk actions**, and **saved views** (your filter/sort combinations, remembered per
device). The current page, sort, search, and filters live in the **URL**, so a list is **bookmarkable
and shareable** — copy the link and it reopens to exactly the same view. On a phone the table folds into
cards. Each list also has an **Export** menu — download the current view (filters and sort included) as
**CSV, Excel, or PDF**. The Users admin screen (Administration → Users) shows all of this.

**Card view:** lists with a strong visual (e.g. **Products**) offer a **Table ⇄ Cards** toggle in the
toolbar — switch to an image-forward card grid; your choice is remembered per device.

**Profile picture:** on your profile, click your avatar (or **Change**) to upload an image — it's
center-cropped to a square and optimized on the server before it's stored.

**For developers:** lists are `useDataGrid({ endpoint })` + `<DataGrid columns={…} />` against any
endpoint returning `PagedResult<T>`; forms use `<Field>` + `useSubmitForm` (validation, success toast,
and server error mapping handled for you); file uploads use `<FileUpload>` or `uploadFile`; exports use
the generic `TabularExport`. Designed loading/empty/error states are shared primitives.

### 8.7 Audit log & entity history
Every create, update, and delete — plus security events like sign-in/out — is recorded automatically.
Admins with the **`audit.read`** permission get an **Audit log** under Administration (**`/admin/audit`**):
filter by **who** (the actor), **category, action, or entity type** and a **date range**, search, and sort. Click any entry
to open a **detail panel** showing who did what and when (with IP and trace id tucked away), and a
**field-level before/after diff** — old values struck through in red, new values in green. Sensitive
fields (passwords, security stamps) are redacted at capture, never stored.

Every record also has a **history timeline** at **`/audit/entity/{type}/{id}`**, deep-linked from the
log's detail panel — a chronological feed of everything that happened to that one record. (This same
`<AuditTimeline>` becomes the foundation of the entity activity feed in the Sales demo.)

### 8.8 Dashboard (home)
The home screen is a **customizable widget dashboard**. Hit **Edit** to **drag, resize, add, configure,
and remove** widgets on a responsive grid (it reflows to a single column on phones), then **Save**.
Keep **multiple named dashboards** and switch between them from the dropdown — set one as your
**default**, rename, copy, or delete. Your layout is saved to your account; the first time you visit you
get a sensible **starter dashboard** tailored to your permissions.

**Built-in widgets:** a **Stat** card and a bold **Highlight** card (a headline metric — total users, new
users this week, today's activity, or unread notifications — with a trend delta and a sparkline), a
**Goal** ring (progress toward a target you set), a **Leaderboard** (ranked bars — users by role or
activity by category), **Line / Area / Bar / Pie charts** (user signups over time, users by role, activity
by category), an **Activity feed** (recent audit events), **Recent notifications**, **Quick links** (your
own list of in-app shortcuts), and a **Markdown note**. The picker only offers widgets you have
permission to populate, and every widget renders its own loading / empty / error state.

**For developers:** add a widget by exporting a `WidgetDefinition` from `src/widgets/widgets/*` and
registering it in `src/widgets/registry.ts` — it then appears in the picker. Data widgets read from the
permission-scoped `/api/dashboard` sources; layouts persist as `DashboardLayout` rows.

### 8.9 Webhooks (outgoing)
Admins with the **`webhooks.read`** permission get **Webhooks** under Administration
(**`/admin/webhooks`**). Register an **endpoint** — a payload URL, a generated **signing secret**, and
the **events** it should receive — and NetForge POSTs a signed JSON body there whenever one of those
events fires. Endpoints can be **paused** (kept, but no new deliveries) and edited at any time; the
secret is shown once at creation (you copy it into your receiver) and only ever masked afterward.

Each endpoint has a **delivery log**: every attempt with its status, response code, duration, and the
exact signed payload — expand a row to inspect the request body and response. A delivery that fails is
**retried automatically** on a backoff schedule (5s → 30s → 5m → 1h → 6h) before giving up; you can also
**resend** any delivery or fire a **test ping** to confirm the endpoint is reachable and verifying
signatures. The log refreshes live while attempts are in flight.

**Verifying a delivery:** each request carries `X-NetForge-Event`, `X-NetForge-Delivery`,
`X-NetForge-Timestamp`, and `X-NetForge-Signature: sha256=<hex>` — an HMAC-SHA256 of the raw body using
your endpoint's secret. Recompute the HMAC over the bytes you receive and compare. Out of the box the
platform emits the user-admin events (`user.locked`, `user.unlocked`, `user.deleted`,
`user.roles_changed`); the Sales demo adds order/product events the same way.

**For developers:** publish an event from any handler with `IEventBus.PublishAsync("entity.event",
payload)`; the dispatcher fans it out to matching active subscriptions and queues a signed delivery per
match. Declare new event names as `public const string` on any `*WebhookEvents` class (reflection-
discovered into the catalog, exactly like `*Permissions`) — they appear in the subscription editor with
no central registry to edit.

### 8.10 Sales (demo domain)
A complete example line-of-business domain under **Sales** in the sidebar — the reference for how a real
feature looks built on NetForge's primitives. It's gated by per-area permissions (`orders.*`,
`products.*`, `categories.*`, `customers.*`), so each screen appears only for users who can use it, and a
Bogus seeder fills it with realistic demo data on first run (delete `Features/Sales` + its one line in
`Program.cs` to remove the demo).

- **Orders** — the centre of the domain. Build an order by picking a customer and adding product lines
  (with a live total preview); the server snapshots each product's price and computes subtotal, tax, and
  total. An order advances through a fixed lifecycle — **Draft → Submitted → Paid → Shipped → Delivered**
  (or **Cancelled**) — and the detail page only offers the legal next steps. Download a branded **PDF
  invoice** for any order. Submitting/paying/shipping also fire outgoing webhook events ([§8.9](#89-webhooks-outgoing)).
- **Products** — a searchable, filterable catalog (by category and active status), money-formatted
  prices, **product images** (drag-and-drop upload; the first is the thumbnail), a **Table ⇄ Cards** view
  toggle, **soft delete + restore** (a "Deleted" toggle), and a concurrency-safe editor: if someone else
  saved while you were editing, you get a clear "changed by someone else" message instead of a silent
  overwrite.
- **Categories** — a browsable tree (sub-categories nest under a parent) with product counts and the same
  soft-delete/restore. Each category can carry an **emoji icon + accent color** for an at-a-glance visual.
- **Customers** — a contact list with an optional shipping address; each detail page shows their order
  count and history.
- **Import & export** — every list exports to **CSV/Excel/PDF** (the Export menu). Products and Customers
  also **import** from CSV/Excel: download the template, fill it in, upload — bad rows are reported by
  line number (e.g. unknown category, duplicate SKU) without failing the rest of the batch.
- **Sales dashboard** — add Sales widgets to your home dashboard ([§8.8](#88-dashboard-home)): revenue and order **stats**, a
  6-month **revenue trend**, **top products**, an **order-status** breakdown, and **recent orders**. They
  come pre-placed for anyone who can read orders.
- **Comments & @mentions** — every Sales record's detail page has a **comments** thread beside its
  history. Type **@** to mention a teammate from an autocomplete list; they get a notification linking
  straight back to the record. You can delete your own comments (moderators can delete any).

### 8.11 Multi-tenancy (optional)
NetForge ships with always-on multi-tenancy plumbing that stays **invisible until you turn it on** —
single-tenant developers see no "tenant" anywhere. Flip it with one config setting:

```jsonc
"Tenancy": { "Mode": "MultiTenant", "Resolution": "UserClaim" } // or Subdomain | Path | Header
```

Once on:
- **Tenant catalog** — `/admin/tenants` (host admins, `tenants.*`) lists, creates, and edits tenants;
  each has a name, status (Active/Suspended), **brand colour**, and logo. The default tenant can't be deleted.
- **Per-tenant roles** — a user's roles are scoped to a tenant. The same person can be Admin in one tenant
  and a read-only member in another; their permissions re-project the moment they switch. Role *definitions*
  (in `/admin/roles`) are shared; the *assignment* is per-tenant, managed on each tenant's detail page.
- **Tenant switcher** — a control in the top bar lists the tenants you belong to; switching re-tints the
  app with that tenant's brand colour and reloads into its data.
- **Invitations** — invite someone by email to a tenant with a role. They get a signed link to an
  **accept** page (sign-in required); accepting adds the membership. Pending invitations can be revoked.
- **Isolation** — every tenant-scoped record carries a `TenantId` and is filtered automatically, so one
  tenant never sees another's data. The active tenant always derives from the signed-in user, so a forged
  subdomain or header can't reach a tenant you don't belong to.

Resolution strategies: **UserClaim** (from the signed-in user — best for localhost/SPA), **Subdomain**
(`acme.app.com`), **Path** (`/acme/…`), or **Header** (`X-Tenant-Id`).

### 8.12 Polish & differentiators
- **Install as an app (PWA)** — on a supported browser, NetForge offers an **Install** banner; once
  installed it opens in its own window and the app shell loads even offline (API calls still need a
  connection). When a new version is deployed, a **"new version — reload"** toast appears.
- **First-run tour** — the first time you sign in, a short guided tour points out the navigation, the
  ⌘K command palette, notifications, the theme toggle, and your account menu. Replay it anytime from
  **account menu → Take a tour**.
- **What's new** — the ✨ button in the top bar shows a dot when there's a release you haven't seen;
  it opens the **/changelog** timeline of recent improvements.
- **System health** (admins, `health.read`) — **/admin/health** shows the live status of the database,
  background jobs, and storage, with auto-refresh. For ops, anonymous probes live at **/health/live**
  (liveness) and **/health/ready** (readiness) for load balancers and orchestrators.
- **Abuse protection** — sign-in, registration, and password-reset endpoints are rate-limited per IP to
  blunt brute-force attempts; exceeding the limit returns a clear "too many requests" response with a
  `Retry-After`. (API versioning is also wired and ready for the first versioned endpoint.)

### 8.13 Quality bar & responsiveness
Every feature ships with designed **loading** (skeletons), **empty**, and **error** states — no bare
spinners or raw error JSON — and adapts to phone viewports (sidebar → drawer, tables → cards).

---

## 9. Developer workflows

| Task | How |
|---|---|
| **Run everything** | `dotnet run --project NetForge.Server` (auto-starts the client). |
| **Run the tests** | `dotnet test NetForge.slnx` (unit + integration). Unit only: `dotnet test tests/Tests.Unit`. |
| **Migrations** | Auto-applied on dev boot. Add one: `dotnet ef migrations add <Name> --project NetForge.Server`. |
| **Browse/test the API** | `https://localhost:7000/scalar`. |
| **Inspect jobs** | `https://localhost:7000/hangfire`. |
| **Add a feature / permission / setting / job** | Copy the `_Template` slice and follow the long-form how-tos in [RECIPES.md](RECIPES.md) (quick version in [../CLAUDE.md](../CLAUDE.md); one-screen cheat-sheet in [CONVENTIONS.md](CONVENTIONS.md)). |
| **Add a language** | Drop `src/locales/<code>.json` and add one entry to `src/i18n.config.ts` (`code`, autonym, `dir`). |
| **Switch database** | Change `ConnectionStrings:Default` (PostgreSQL/SQL Server) and re-run migrations. |
| **Scaffold a fresh app from this template** | `dotnet new netforge -n MyApp` — toggles: `--tier` (`pro`/`basic`), `--IncludeDemo`, `--Google`/`--Microsoft`/`--GitHub`. See the [configurator reference](https://docs.netforge.ebenmonney.com/reference/configurator). |

---

## 10. Tech stack

**Backend:** .NET 10 · Minimal APIs · EF Core 10 · ASP.NET Identity · FluentValidation · Serilog ·
Hangfire · Scalar (API docs) · SignalR · Magick.NET (images) · ClosedXML + CsvHelper + QuestPDF (export).
**Frontend:** React 19 · Vite 8 · TypeScript 6 · Tailwind v4 · shadcn/ui · React Router 7 (file-system
routes) · TanStack Query + Table · Zustand · React Hook Form + Zod · sonner · lucide-react · `@microsoft/signalr`.
**Database:** SQLite (dev) · PostgreSQL (prod default) · SQL Server (supported).
**Testing:** xUnit v3 · Shouldly · NSubstitute · `WebApplicationFactory<Program>` (integration over throwaway SQLite).

---

## 11. Learn more

- **[RECIPES.md](RECIPES.md)** — copy-pasteable how-tos for extending the app (add a feature, widget, webhook, language, …).
- **[CONVENTIONS.md](CONVENTIONS.md)** — the one-screen conventions cheat-sheet.
- **[../CLAUDE.md](../CLAUDE.md)** — the canonical agent/contributor guide (architecture, conventions, what to do).
- **[docs.netforge.ebenmonney.com](https://docs.netforge.ebenmonney.com)** — the full online documentation, with a tier-aware feature reference and the editions breakdown.
