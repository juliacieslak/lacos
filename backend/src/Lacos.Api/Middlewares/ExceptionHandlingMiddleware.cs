using Lacos.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lacos.Api.Middlewares;

/// <summary>
/// Tratamento global de exceções. Converte cada tipo de exceção em resposta HTTP padronizada
/// (RFC 7807 - ProblemDetails) com um "code" estável para o front-end.
///
/// Regras de segurança: detalhes internos (stack trace, SQL, mensagens de infraestrutura) nunca são
/// devolvidos ao cliente; ficam apenas no log estruturado, correlacionados pelo traceId.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Cliente cancelou a requisição: nada a responder.
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, code, title, detail) = ex switch
        {
            ValidationFailedException v => (400, v.Code, "Dados inválidos", v.Message),
            NotFoundException n => (404, n.Code, "Não encontrado", n.Message),
            UserNotRegisteredException u => (403, u.Code, "Usuário não registrado", u.Message),
            ForbiddenException f => (403, f.Code, "Acesso negado", f.Message),
            ConflictException c => (409, c.Code, "Conflito", c.Message),
            BusinessRuleException b => (422, b.Code, "Regra de negócio violada", b.Message),
            ExternalServiceException e => (503, e.Code, "Serviço externo indisponível", e.Message),
            DbUpdateConcurrencyException => (409, "CONCURRENCY_CONFLICT", "Conflito", "O registro foi alterado por outro usuário. Recarregue e tente novamente."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (409, "DUPLICATED_RECORD", "Conflito", "Já existe um registro com estes dados."),
            BadHttpRequestException bad => (bad.StatusCode, "BAD_REQUEST", "Requisição inválida", "A requisição não pôde ser processada."),
            ArgumentException arg => (400, "INVALID_ARGUMENT", "Dados inválidos", arg.Message),
            _ => (500, "INTERNAL_ERROR", "Erro interno", "Ocorreu um erro inesperado. Tente novamente mais tarde.")
        };

        if (status >= 500) _logger.LogError(ex, "Erro não tratado em {Method} {Path}", context.Request.Method, context.Request.Path);
        else _logger.LogWarning("Requisição rejeitada ({Status} {Code}) em {Method} {Path}: {Message}",
            status, code, context.Request.Method, context.Request.Path, ex.Message);

        if (context.Response.HasStarted) return;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.io/{status}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (ex is ValidationFailedException validation) problem.Extensions["errors"] = validation.Errors;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }
}
