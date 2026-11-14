using FluentValidation;

namespace AgroMarket.Api.Common;

/// <summary>
/// Endpoint filter that validates the first matching request argument of type <typeparamref name="TRequest"/>
/// using FluentValidation before the minimal API handler executes.
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await next(context);
        }

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return TypedResults.Problem(
                title: "Invalid request payload",
                detail: $"Expected a request body of type {typeof(TRequest).Name}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}

public static class RouteHandlerBuilderValidationExtensions
{
    /// <summary>
    /// Attaches <see cref="ValidationFilter{TRequest}"/> to the endpoint and documents 400 validation errors.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
    }
}
