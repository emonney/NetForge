namespace NetForge.Server.Platform.Email;

/// <summary>
/// Renders a polished, brand-accented HTML email — table-based and inline-styled for broad mail-client
/// support (Outlook included; no external CSS). Shared by the auth + tenancy transactional emails so they
/// all look like one product. The accent comes from the instance brand colour, with a neutral fallback.
/// </summary>
public static class EmailLayout
{
    // Indigo — used when no usable brand colour is configured (blank, or the unreplaced scaffold token).
    private const string FallbackAccent = "#4f46e5";

    public static string Render(
        string product, string? brandColor, string heading, string body,
        string ctaText, string ctaHref, string? footerNote = null)
    {
        var accent = Accent(brandColor);
        var year = DateTime.UtcNow.Year;
        var note = footerNote ?? "If you didn't request this, you can safely ignore this email.";
        var href = H(ctaHref);
        return $$"""
        <!doctype html>
        <html lang="en">
          <body style="margin:0;padding:0;background:#f1f5f9;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:32px 12px;">
              <tr><td align="center">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;">
                  <tr><td style="background:{{accent}};height:6px;font-size:0;line-height:0;">&nbsp;</td></tr>
                  <tr><td style="padding:32px 32px 0;font-family:system-ui,-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    <div style="font-size:18px;font-weight:700;color:#0f172a;letter-spacing:-0.01em;">{{H(product)}}</div>
                  </td></tr>
                  <tr><td style="padding:24px 32px 0;font-family:system-ui,-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    <h1 style="font-size:20px;font-weight:600;color:#0f172a;margin:0 0 10px;">{{H(heading)}}</h1>
                    <p style="font-size:15px;line-height:1.65;color:#475569;margin:0;">{{H(body)}}</p>
                  </td></tr>
                  <tr><td style="padding:28px 32px 4px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    <a href="{{href}}" style="display:inline-block;background:{{accent}};color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:9px;font-size:15px;font-weight:600;">{{H(ctaText)}}</a>
                  </td></tr>
                  <tr><td style="padding:12px 32px 28px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    <p style="font-size:12px;line-height:1.6;color:#94a3b8;margin:16px 0 0;">{{H(note)}}</p>
                    <p style="font-size:12px;line-height:1.6;color:#cbd5e1;margin:8px 0 0;word-break:break-all;">{{href}}</p>
                  </td></tr>
                  <tr><td style="padding:18px 32px;background:#f8fafc;border-top:1px solid #e2e8f0;font-family:system-ui,-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    <p style="font-size:11px;color:#94a3b8;margin:0;">&copy; {{year}} {{H(product)}}</p>
                  </td></tr>
                </table>
              </td></tr>
            </table>
          </body>
        </html>
        """;
    }

    private static string Accent(string? color) => IsUsableColor(color) ? color!.Trim() : FallbackAccent;

    // A usable inline-style colour: hex, rgb()/hsl(), or a plain CSS keyword. Rejects the unreplaced scaffold
    // token and anything with characters that have no place in a style value (defence against style injection).
    private static bool IsUsableColor(string? color)
    {
        var c = color?.Trim();
        if (string.IsNullOrEmpty(c) || c.Length > 32) return false;
        return c.All(ch => char.IsLetterOrDigit(ch) || ch is '#' or '(' or ')' or ',' or '%' or '.' or ' ');
    }

    // Minimal HTML-escape for interpolated values: product/heading/body are app-controlled, but a user's
    // email address / display name can reach these paths, so never inject them raw.
    private static string H(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
