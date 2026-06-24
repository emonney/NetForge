namespace NetForge.Server.Platform;

/// <summary>
/// App-wide identity/branding bound from the "App" configuration section. <see cref="ClientUrl"/>
/// is the public SPA origin used to build links in outgoing emails; when null we fall back to the
/// incoming request's origin (correct in production where SPA + API share an origin).
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    public string ProductName { get; set; } = "NetForge";

    public string? ClientUrl { get; set; }

    /// <summary>Brand accent for transactional emails (any CSS colour). Defaults to the scaffold brand colour;
    /// the email layout falls back to a neutral accent when this isn't a usable colour (blank, or the
    /// unreplaced template token in the source repo).</summary>
    public string? BrandColor { get; set; } = "";

    /// <summary>Optional shared demo credentials surfaced on the sign-in screen as a click-to-fill chip
    /// (e.g. a public demo site). Configured via "App:DemoLogin"; null in a normal app, so nothing is shown.</summary>
    public DemoLoginInfo? DemoLogin { get; set; }
}

/// <summary>Shared demo sign-in credentials, surfaced (intentionally public) on the login screen.</summary>
public sealed class DemoLoginInfo
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}
