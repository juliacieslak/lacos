using FluentValidation;
using Lacos.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lacos.Api.Filters;

/// <summary>
/// Executa automaticamente o validador FluentValidation de cada argumento da action (DTOs de requisição).
/// Falhas são convertidas em <see cref="ValidationFailedException"/> → HTTP 400 com os erros por campo.
/// Assim, os controllers ficam enxutos e nenhuma requisição inválida chega à camada de aplicação.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var argument in context.ActionArguments.Values.Where(a => a is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            if (_services.GetService(validatorType) is not IValidator validator) continue;
            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            foreach (var group in result.Errors.GroupBy(e => ToCamelCase(e.PropertyName)))
                errors[group.Key] = group.Select(e => e.ErrorMessage).Distinct().ToArray();
        }
        if (errors.Count > 0) throw new ValidationFailedException(errors);
        await next();
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
