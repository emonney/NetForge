using NetForge.Server.Platform;

namespace NetForge.Server.Features.Auth;

internal static class AuthUrls
{
    /// <summary>SPA origin for links/redirects: configured App:ClientUrl, else the request origin
    /// (correct in production where SPA + API share an origin).</summary>
    public static string ClientBaseUrl(HttpContext http, AppOptions options) =>
        (string.IsNullOrWhiteSpace(options.ClientUrl)
            ? $"{http.Request.Scheme}://{http.Request.Host}"
            : options.ClientUrl).TrimEnd('/');

    /// <summary>Only allow local relative paths as a post-login redirect — blocks open redirects.</summary>
    public static string SafeReturnPath(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? returnUrl
            : "/";
}
