namespace Lacos.Application.Interfaces;

/// <summary>
/// Identidade da requisição atual, extraída do token JWT emitido pelo Auth0 e do contexto HTTP.
/// Abstração para que a camada de aplicação não dependa de ASP.NET Core (Dependency Inversion).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    /// <summary>Claim "sub" do Auth0 (users.auth0_user_id).</summary>
    string? Auth0UserId { get; }
    string? Email { get; }
    bool EmailVerified { get; }
    string? Name { get; }
    /// <summary>IP de origem (RNF04 exige IP na trilha de auditoria).</summary>
    string? IpAddress { get; }
    string? UserAgent { get; }
}

/// <summary>Relógio do sistema e fuso horário de negócio (facilita testes determinísticos).</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    /// <summary>Fuso horário usado para interpretar horários de medicamentos, rotinas e "hoje" na agenda.</summary>
    TimeZoneInfo TimeZone { get; }
    /// <summary>Data local de hoje no fuso de negócio.</summary>
    DateOnly LocalToday { get; }
    /// <summary>Converte data + hora locais em instante absoluto.</summary>
    DateTimeOffset ToInstant(DateOnly date, TimeOnly time);
    /// <summary>Converte um instante para a data local.</summary>
    DateOnly ToLocalDate(DateTimeOffset instant);
}
