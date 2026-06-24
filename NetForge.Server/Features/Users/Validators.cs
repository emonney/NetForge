using FluentValidation;

namespace NetForge.Server.Features.Users;

public sealed class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotNull().WithMessage("Roles are required.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");
    }
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");
        // The password is optional (the invite path sets it later); Identity's policy enforces strength
        // when one is supplied, so only the bare minimum is checked here to fail fast on the obvious case.
        RuleFor(x => x.Password!).MinimumLength(6)
            .When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 6 characters.");
    }
}
