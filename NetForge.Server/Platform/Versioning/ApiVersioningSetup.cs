using Asp.Versioning;

namespace NetForge.Server.Platform.Versioning;

/// <summary>
/// API versioning is wired but unused: the infrastructure is registered with a default v1.0 so the
/// whole existing surface keeps working unversioned, and a slice can opt a route group into explicit
/// versioning the day it needs to — without a breaking rename or a Program.cs edit.
///
/// Recipe (in a slice's <c>IFeatureEndpoints.Map</c>):
/// <code>
/// var api = app.NewVersionedApi("Widgets");
/// var v1 = api.MapGroup("/api/widgets").HasApiVersion(ApiVersioningSetup.V1);
/// v1.MapGet("", ListV1);
/// // a future v2 lives side-by-side: api.MapGroup("/api/widgets").HasApiVersion(ApiVersioningSetup.V2)
/// </code>
/// Clients select a version via the URL segment, the <c>X-Api-Version</c> header, or the
/// <c>?api-version=</c> query string; responses advertise supported versions in <c>api-supported-versions</c>.
/// </summary>
public static class ApiVersioningSetup
{
    /// <summary>The current API version. The default for any endpoint that doesn't ask for another.</summary>
    public static readonly ApiVersion V1 = new(1, 0);

    public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = V1;
            // Existing slices declare no version; treat them as v1 so nothing 400s for a missing version.
            options.AssumeDefaultVersionWhenUnspecified = true;
            // Advertise api-supported-versions / api-deprecated-versions on versioned responses.
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        });

        return services;
    }
}
