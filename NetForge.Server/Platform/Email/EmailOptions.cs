namespace NetForge.Server.Platform.Email;

/// <summary>
/// Outgoing email configuration, bound from the "Email" section. When <see cref="SmtpOptions.Host"/> and
/// <see cref="FromAddress"/> are both set the app sends via SMTP (MailKit, <see cref="SmtpEmailSender"/>);
/// otherwise it logs each message with a NOT-SENT warning (<see cref="DevConsoleEmailSender"/>). Works with
/// any SMTP relay — Brevo, SendGrid, Mailgun, Gmail, or your own server.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Display name on the From header; falls back to the bare address when blank.</summary>
    public string? FromName { get; set; }

    /// <summary>From address (e.g. no-reply@yourapp.com). Required — with a host — to enable SMTP sending.</summary>
    public string? FromAddress { get; set; }

    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>True once a host + from address are present, which switches the app from logging to sending.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Smtp.Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>STARTTLS on a submission port (587 — Brevo and most relays). Set false for implicit TLS (465).</summary>
    public bool UseStartTls { get; set; } = true;
}
