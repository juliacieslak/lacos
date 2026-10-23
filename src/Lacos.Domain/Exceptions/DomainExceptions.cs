namespace Lacos.Domain.Exceptions;

/// <summary>
/// Exceção base para violações de regras de negócio.
/// O middleware global de exceções converte cada subtipo em um status HTTP apropriado
/// e em um corpo ProblemDetails com o <see cref="Code"/> para que o front-end trate o erro.
/// </summary>
public abstract class LacosException : Exception
{
    protected LacosException(string code, string message) : base(message) => Code = code;

    /// <summary>Código estável e legível por máquina (ex.: PERMISSION_DENIED).</summary>
    public string Code { get; }
}

/// <summary>Regra de negócio violada (HTTP 422).</summary>
public sealed class BusinessRuleException : LacosException
{
    public BusinessRuleException(string code, string message) : base(code, message) { }
}

/// <summary>Recurso não encontrado ou não visível ao usuário (HTTP 404).</summary>
public sealed class NotFoundException : LacosException
{
    public NotFoundException(string resource, object key)
        : base("NOT_FOUND", $"{resource} '{key}' não foi encontrado(a).") { }
}

/// <summary>Usuário autenticado sem permissão para a operação (HTTP 403).</summary>
public sealed class ForbiddenException : LacosException
{
    public ForbiddenException(string message, string code = "PERMISSION_DENIED") : base(code, message) { }
}

/// <summary>Conflito com o estado atual (ex.: CPF já cadastrado) (HTTP 409).</summary>
public sealed class ConflictException : LacosException
{
    public ConflictException(string code, string message) : base(code, message) { }
}

/// <summary>Falha de validação de entrada com detalhes por campo (HTTP 400).</summary>
public sealed class ValidationFailedException : LacosException
{
    public ValidationFailedException(IDictionary<string, string[]> errors)
        : base("VALIDATION_FAILED", "Um ou mais campos são inválidos.") => Errors = errors;

    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } }) { }

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>Usuário autenticado no provedor de identidade mas ainda não registrado/ativo no sistema (HTTP 403).</summary>
public sealed class UserNotRegisteredException : LacosException
{
    public UserNotRegisteredException()
        : base("USER_NOT_REGISTERED", "Usuário não registrado. Chame POST /api/auth/session após o login.") { }
}

/// <summary>Serviço externo indisponível ou não configurado (HTTP 503).</summary>
public sealed class ExternalServiceException : LacosException
{
    public ExternalServiceException(string service, string message) : base("EXTERNAL_SERVICE_UNAVAILABLE", $"{service}: {message}") { }
}
