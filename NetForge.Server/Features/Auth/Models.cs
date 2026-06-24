namespace NetForge.Server.Features.Auth;

// Requests
public record RegisterRequest(string Email, string Password, string? DisplayName);

public record LoginRequest(string Email, string Password, bool RememberMe = false);

public record ConfirmEmailRequest(string UserId, string Token);

public record ResendConfirmationRequest(string Email);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

public record UpdateProfileRequest(string? DisplayName);

/// <summary>User-scoped preferences edited from the profile. Separate from the profile-info update so
/// saving one never clobbers the other.</summary>
public record UpdatePreferencesRequest(string? Locale, string? TimeZone);

// Responses

/// <summary>The authenticated identity the SPA renders from. <see cref="Permissions"/> is the
/// effective grant set (union across roles, wildcards included) the UI gates on.</summary>
public record AuthUserDto(
    string Id,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string? Locale,
    string? TimeZone,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool HasPassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>
/// Login outcome. When <see cref="RequiresTwoFactor"/> is true the password step succeeded but
/// the cookie is a partial 2FA cookie — the client must complete the TOTP step. Otherwise
/// <see cref="User"/> is the signed-in identity.
/// </summary>
public record LoginResultDto(bool RequiresTwoFactor, AuthUserDto? User);

// --- Two-factor (TOTP) ---

/// <summary>Authenticator enrolment payload: the raw secret to seed an app, formatted for manual
/// entry, plus the otpauth:// URI a QR code encodes.</summary>
public record TwoFactorSetupDto(string SharedKey, string AuthenticatorUri);

public record EnableTwoFactorRequest(string Code);

/// <summary>One-time recovery codes shown exactly once, right after enabling 2FA or regenerating.</summary>
public record RecoveryCodesDto(IReadOnlyList<string> RecoveryCodes);

public record TwoFactorLoginRequest(string Code, bool RememberMachine = false, bool RememberMe = false);

public record RecoveryCodeLoginRequest(string RecoveryCode, bool RememberMe = false);

// --- Active sessions ---

/// <summary>A signed-in device. <see cref="Current"/> marks the session making the request.</summary>
public record SessionDto(
    string Id,
    string? DeviceName,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    bool Current);
