using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NetForge.Server.Data;
using NetForge.Server.Platform.Email;

namespace NetForge.Server.Features.Auth;

/// <summary>
/// Composes and sends the transactional auth emails. Identity tokens contain characters that
/// aren't URL-safe, so they're Base64Url-encoded into the link and decoded on the way back in.
/// Links target the SPA origin (<paramref name="clientBaseUrl"/>), not the API.
/// </summary>
internal static class AuthEmails
{
    public static Task SendEmailConfirmationAsync(
        IEmailSender email, AppUser user, string token, string clientBaseUrl, string product, string? brandColor,
        CancellationToken ct)
    {
        var link = $"{clientBaseUrl}/verify-email?userId={Uri.EscapeDataString(user.Id)}&token={Encode(token)}";
        var html = EmailLayout.Render(product, brandColor,
            "Confirm your email",
            $"Welcome to {product}! Confirm your email address to activate your account.",
            "Confirm email", link);
        return email.SendAsync(
            new EmailMessage(user.Email!, $"Confirm your {product} account", html,
                TextBody: $"Confirm your {product} account: {link}"), ct);
    }

    public static Task SendPasswordResetAsync(
        IEmailSender email, AppUser user, string token, string clientBaseUrl, string product, string? brandColor,
        CancellationToken ct)
    {
        var link = $"{clientBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Encode(token)}";
        var html = EmailLayout.Render(product, brandColor,
            "Reset your password",
            "We received a request to reset your password. This link expires shortly. If you didn't ask for this, you can ignore this email.",
            "Reset password", link);
        return email.SendAsync(
            new EmailMessage(user.Email!, $"Reset your {product} password", html,
                TextBody: $"Reset your {product} password: {link}"), ct);
    }

    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
}
