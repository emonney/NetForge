namespace NetForge.Server.Platform.Email;

/// <summary>
/// Development email sender — logs the message with a loud NOT-SENT warning so emails are
/// never silently swallowed. Replaced by a MailKit/SMTP impl in production config.
/// </summary>
public sealed class DevConsoleEmailSender(ILogger<DevConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "DEV MODE — EMAIL NOT SENT. To: {To} | Subject: {Subject}\n{Body}",
            message.To, message.Subject, message.TextBody ?? message.HtmlBody);
        return Task.CompletedTask;
    }
}
