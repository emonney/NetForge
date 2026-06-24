using FluentValidation;

namespace NetForge.Server.Features.Comments;

internal sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().WithMessage("Write something first.").MaximumLength(4000);
        RuleFor(x => x.Url).MaximumLength(2000);
    }
}
