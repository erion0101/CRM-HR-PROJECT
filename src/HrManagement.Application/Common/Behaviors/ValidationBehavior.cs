using FluentValidation;
using MediatR;
using ApplicationValidationException = HrManagement.Application.Common.Exceptions.ValidationException;

namespace HrManagement.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken)));
        var errors = results.SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);

        if (errors.Count > 0) throw new ApplicationValidationException(errors);
        return await next();
    }
}
