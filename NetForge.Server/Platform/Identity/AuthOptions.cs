namespace NetForge.Server.Platform.Identity;

/// <summary>
/// Active authentication scheme. Cookie is the default; Bearer is registered but inactive
/// until a dev flips <see cref="AuthOptions.Scheme"/> (see CLAUDE.md "Bearer-mode auth").
/// </summary>
public enum AuthScheme
{
    Cookie,
    Bearer,
}

/// <summary>
/// Bound from the "Auth" configuration section. Controls the auth scheme, sign-in policy,
/// and OAuth provider credentials. Each OAuth block is optional — a provider with no
/// credentials is never registered, and its button auto-hides on the login page.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public AuthScheme Scheme { get; set; } = AuthScheme.Cookie;

    /// <summary>Require a confirmed email before first sign-in. Dev seeds a pre-confirmed admin.</summary>
    public bool RequireConfirmedEmail { get; set; } = true;

    public OAuthProviders OAuth { get; set; } = new();
}

public sealed class OAuthProviders
{
    public OAuthProviderOptions? Google { get; set; }
    public OAuthProviderOptions? Microsoft { get; set; }
    public OAuthProviderOptions? GitHub { get; set; }
}

public sealed class OAuthProviderOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>A provider is live only when both credentials are present.</summary>
    public bool Configured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
