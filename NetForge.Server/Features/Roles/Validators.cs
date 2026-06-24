using FluentValidation;
using NetForge.Server.Platform.Authorization;

namespace NetForge.Server.Features.Roles;

public sealed class SaveRoleRequestValidator : AbstractValidator<SaveRoleRequest>
{
    public SaveRoleRequestValidator(PermissionCatalog catalog)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(256);

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("Permissions are required.");

        RuleForEach(x => x.Permissions)
            .Must(catalog.IsAssignable)
            .WithMessage("'{PropertyValue}' is not a known permission.");
    }
}
