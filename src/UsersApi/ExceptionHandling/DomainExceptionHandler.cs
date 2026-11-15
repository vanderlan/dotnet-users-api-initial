using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UsersApi.Domain.Exceptions;

namespace UsersApi.ExceptionHandling;

/// <summary>
/// Traduz exceções de domínio em respostas Problem Details (RFC 9457, application/problem+json).
/// Plugado por app.UseExceptionHandler(). Qualquer exceção desconhecida vira 500 genérico:
/// detalhes internos são logados, nunca enviados ao cliente.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Erro não tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("{Method} {Path} falhou com {Status}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode, exception.Message);

        var problem = exception is BusinessException business
            ? new ProblemDetails { Status = statusCode, Title = business.Title, Detail = business.Message }
            : new ProblemDetails
            {
                Status = statusCode,
                Title = "Erro inesperado",
                Detail = "Ocorreu um erro inesperado. Tente novamente mais tarde."
            };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
