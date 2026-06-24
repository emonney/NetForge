namespace NetForge.Server.Platform.Errors;

/// <summary>
/// Base for expected domain failures. The global handler maps these to RFC 7807
/// ProblemDetails. Never throw raw <see cref="Exception"/> for control flow — throw a
/// subclass of this so callers get a typed status, code, and (optionally) field errors.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }

    public abstract int Status { get; }
    public abstract string Code { get; }

    /// <summary>Per-field errors for validation failures: field name → messages.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; protected init; }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message = "The requested resource was not found.") : base(message) { }
    public NotFoundException(string resource, object key) : base($"{resource} '{key}' was not found.") { }

    public override int Status => StatusCodes.Status404NotFound;
    public override string Code => "NOT_FOUND";
}

public sealed class BadRequestException(string message) : DomainException(message)
{
    public override int Status => StatusCodes.Status400BadRequest;
    public override string Code => "BAD_REQUEST";
}

public sealed class ConflictException(string message = "The request conflicts with the current state of the resource.")
    : DomainException(message)
{
    public override int Status => StatusCodes.Status409Conflict;
    public override string Code => "CONFLICT";
}

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : DomainException(message)
{
    public override int Status => StatusCodes.Status403Forbidden;
    public override string Code => "FORBIDDEN";
}

/// <summary>
/// Authentication failure (401). Carries a specific <see cref="Code"/> (e.g.
/// INVALID_CREDENTIALS, EMAIL_NOT_CONFIRMED, ACCOUNT_LOCKED) so the client can branch
/// on the cause without parsing the message.
/// </summary>
public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message, string code = "UNAUTHENTICATED") : base(message) => Code = code;

    public override int Status => StatusCodes.Status401Unauthorized;
    public override string Code { get; }
}

public sealed class ValidationException : DomainException
{
    public ValidationException(
        IReadOnlyDictionary<string, string[]> errors,
        string message = "One or more fields failed validation.")
        : base(message) => Errors = errors;

    public override int Status => StatusCodes.Status400BadRequest;
    public override string Code => "VALIDATION_FAILED";
}
