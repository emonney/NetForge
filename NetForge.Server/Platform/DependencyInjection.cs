using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetForge.Server.Platform.Auditing;
using NetForge.Server.Platform.Caching;
using NetForge.Server.Platform.Email;
using NetForge.Server.Platform.Errors;
using NetForge.Server.Platform.Features;
using NetForge.Server.Platform.Health;
using NetForge.Server.Platform.Identity;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Server.Platform.RateLimiting;
using NetForge.Server.Platform.Settings;
using NetForge.Server.Platform.Versioning;

namespace NetForge.Server.Platform;

/// <summary>
/// Single composition point for platform services so Program.cs stays a thin root.
/// Each platform capability registers itself here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {

        // App-wide branding/origin (product name, client URL for email links).
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        // Errors — RFC 7807 ProblemDetails for every failure.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // API versioning — wired with a default v1.0 but unused: every slice stays unversioned until it
        // opts a route group in. Nothing forces a version today; the infrastructure is just ready.
        services.AddApiVersioningSupport();

        // Rate limiting — a generous global net over /api/* plus named policies (strict per-IP "auth"
        // for credential endpoints; opt-in "api" for expensive groups). Rejections are ProblemDetails.
        services.AddRateLimitingSupport();

        // Health checks — database/jobs/storage probes feed both the anonymous ops endpoints
        // (/health/live, /health/ready) and the permission-gated /admin/health dashboard.
        services.AddHealthChecksSupport();

        // Validation — FluentValidation validators discovered per slice; run by ValidationFilter.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        // Identity & auth — cookie scheme (default) + bearer (inactive) + OAuth (config-gated).
        services.AddPlatformIdentity(configuration);

        // Multi-tenancy — always-on infrastructure; single-tenant by default (invisible).
        services.AddHttpContextAccessor();
        services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantRoleService, TenantRoleService>();
        services.AddScoped<TenantInterceptor>();

        // Caching — in-memory with tagged invalidation (Redis swaps in later).
        services.AddMemoryCache();
        services.AddSingleton<ICache, MemoryCacheService>();


        // Email — SMTP via MailKit when configured (Email:Smtp:Host + Email:FromAddress), else a dev sender
        // that logs a NOT-SENT warning so messages are never silently dropped. Any SMTP relay works (Brevo,
        // SendGrid, Mailgun, Gmail, your own server).
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (emailOptions.IsConfigured)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, DevConsoleEmailSender>();

        // Persist Data-Protection keys to App_Data so auth cookies (and other protected payloads) survive app
        // restarts + app-pool recycles on single-server / shared hosting — the default per-profile location is
        // often wiped on recycle, which silently signs everyone out. Multi-instance deployments should point
        // this at a shared store (Redis, blob storage, etc.) instead.
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "App_Data", "keys")))
            .SetApplicationName("NetForge");



        // Feature services — each slice registers its own DI bindings via IServiceRegistrar
        // (reflection), so a feature service (e.g. INotificationService) needs no Program.cs edit.
        services.AddFeatureServices();

        // Editions without the Webhooks feature get a no-op event bus so slices keep calling
        // PublishAsync unconditionally (Pro registers the real dispatcher via the Webhooks slice above,
        // in AddFeatureServices). TryAdd, not Add: in the live repo the conditional comment guards are
        // inert, so this line also executes — TryAdd makes it defer to the real bus, not shadow it.
        services.TryAddSingleton<NetForge.Server.Platform.Webhooks.IEventBus, NetForge.Server.Platform.Webhooks.NoopEventBus>();


        // Settings — User→Tenant→App resolution, cached with tag invalidation. Feature settings
        // self-register via ISettingsContributor (reflection) into the static definition registry.
        services.AddScoped<ISettingService, SettingService>();
        SettingsRegistration.RegisterAll();

        // Choice settings can supply dynamic dropdown options (e.g. roles) via ISettingOptionsProvider —
        // reflection-discovered + registered so the settings UI resolves them without hard-coding.
        foreach (var provider in typeof(ISettingOptionsProvider).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ISettingOptionsProvider).IsAssignableFrom(t)))
            services.AddScoped(typeof(ISettingOptionsProvider), provider);

        // Editions without the Audit feature: handlers still call IAuditService.LogAsync — no-op it.
        // TryAdd, not Add: in the live repo the conditional comment guards are inert, so this runs
        // alongside the real AuditService above — TryAdd defers to it instead of shadowing it (which had
        // silently turned every explicit audit, e.g. login, into a no-op).
        services.TryAddScoped<IAuditService, NoopAuditService>();

        return services;
    }
}
