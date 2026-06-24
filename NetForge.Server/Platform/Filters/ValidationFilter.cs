using FluentValidation;
using ValidationException = NetForge.Server.Platform.Errors.ValidationException;

namespace NetForge.Server.Platform.Filters;

/// <summary>
/// Finds the request argument, resolves its <see cref="IValidator{T}"/>, runs it, and
/// throws <see cref="ValidationException"/> (→ 400 ProblemDetails with field errors) on
/// failure. Apply per group — args without a registered validator pass through.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (result.IsValid) continue;

            var errors = result.Errors
                .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        return await next(context);
    }
}
