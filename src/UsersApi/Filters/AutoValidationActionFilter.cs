using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UsersApi.Filters;

/// <summary>
/// Action Filter: roda ANTES da action do controller. Valida cada argumento (DTO) com o
/// FluentValidation e, se algo estiver errado, responde 400 sem nem chamar o controller.
/// </summary>
public sealed class AutoValidationActionFilter(
    IServiceProvider services,
    ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var modelState = new ModelStateDictionary();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            // Procura um IValidator<TipoDoArgumento> registrado no container de DI.
            var validatorType = typeof(IValidator<>)
                .MakeGenericType(argument.GetType());

            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (modelState.ErrorCount > 0)
        {
            var problem = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext, modelState,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Erro de validação");

            context.Result = new BadRequestObjectResult(problem);
            return; // curto-circuito: a action não é executada
        }

        await next(); // segue o pipeline
    }
}
