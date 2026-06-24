using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NetForge.Server.Platform.Email;

/// <summary>
/// Sends transactional email over SMTP via MailKit. Registered automatically when <see cref="EmailOptions"/>
/// is configured (a host + from address); otherwise the dev console sender is used. A fresh connection per
/// message keeps it simple and stateless — fine for the app's transactional volume.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // Guaranteed by EmailOptions.IsConfigured (the only path that registers this sender), but assert so a
        // misconfiguration surfaces as a clear error rather than a null-reference deep inside MailKit.
        var fromAddress = message.From ?? _options.FromAddress
            ?? throw new InvalidOperationException("Email:FromAddress must be configured to send email.");
        var smtp = _options.Smtp;
        var host = smtp.Host
            ?? throw new InvalidOperationException("Email:Smtp:Host must be configured to send email.");

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName ?? string.Empty, fromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, smtp.Port,
            smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect, cancellationToken);
        if (!string.IsNullOrWhiteSpace(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
    }
}
