using Microsoft.Extensions.Options;

namespace NetForge.Server.Platform.MultiTenancy;

/// <summary>Resolves the current request's tenant. Single-tenant mode returns a constant.</summary>
public interface ITenantContext
{
    string TenantId { get; }
    bool IsMultiTenant { get; }
}

public sealed class TenantContext(IOptions<TenancyOptions> options, IHttpContextAccessor httpContextAccessor)
    : ITenantContext
{
    /// <summary>Key under which <see cref="TenantResolutionMiddleware"/> stashes the resolved tenant.</summary>
    public const string ItemKey = "NetForge.TenantId";

    private readonly TenancyOptions _options = options.Value;

    public bool IsMultiTenant => _options.Mode == TenancyMode.MultiTenant;

    // The middleware resolves the tenant once per request into HttpContext.Items. Outside a request
    // (background jobs, the seeder, design-time) there's no HttpContext, so fall back to the default —
    // such callers query by explicit tenant id rather than relying on the ambient one.
    public string TenantId =>
        httpContextAccessor.HttpContext?.Items.TryGetValue(ItemKey, out var value) == true
        && value is string tenantId && tenantId.Length > 0
            ? tenantId
            : TenancyOptions.DefaultTenant;
}
