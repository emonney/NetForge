namespace NetForge.Server.Platform.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null,
    string? From = null);

/// <summary>
/// Email abstraction. The dev impl writes to the log with a loud NOT-SENT warning;
/// the production MailKit/SMTP impl + Razor templates land in a later phase.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
