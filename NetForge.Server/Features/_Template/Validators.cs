using FluentValidation;

namespace NetForge.Server.Features._Template;

// One AbstractValidator per request type. The ValidationFilter resolves and runs these
// automatically, returning 400 ProblemDetails with field errors on failure.
internal sealed class CreateTemplateItemValidator : AbstractValidator<CreateTemplateItemRequest>
{
    public CreateTemplateItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

internal sealed class UpdateTemplateItemValidator : AbstractValidator<UpdateTemplateItemRequest>
{
    public UpdateTemplateItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
