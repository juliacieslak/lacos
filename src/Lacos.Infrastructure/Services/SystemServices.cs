using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Services;

/// <summary>Configuração do fuso horário de negócio (horários de medicação, rotina e "hoje").</summary>
public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";
    /// <summary>SUPOSIÇÃO: aplicação brasileira; padrão America/Sao_Paulo (IANA).</summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";
}

/// <summary>Relógio do sistema com conversões de fuso horário.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public SystemDateTimeProvider(IOptions<SchedulingOptions> options)
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo TimeZone { get; }
    public DateOnly LocalToday => ToLocalDate(UtcNow);

    /// <summary>
    /// Converte data+hora locais para instante absoluto considerando o deslocamento vigente naquela data
    /// (correto mesmo se houver horário de verão).
    /// </summary>
    public DateTimeOffset ToInstant(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = TimeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    public DateOnly ToLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZone).DateTime);
}

/// <summary>
/// Gera os identificadores públicos via sequências do PostgreSQL (RF17): "U" + 6 dígitos para usuários e
/// "P" + 6 dígitos para pacientes. Sequências são atômicas e seguras para várias instâncias simultâneas.
/// </summary>
public sealed class PostgresPublicCodeGenerator : IPublicCodeGenerator
{
    private readonly LacosDbContext _context;

    public PostgresPublicCodeGenerator(LacosDbContext context) => _context = context;

    public Task<string> NextUserCodeAsync(CancellationToken ct = default) => NextAsync("user_public_code_seq", "U", ct);
    public Task<string> NextPatientCodeAsync(CancellationToken ct = default) => NextAsync("patient_public_code_seq", "P", ct);

    private async Task<string> NextAsync(string sequence, string prefix, CancellationToken ct)
    {
        // O nome da sequência vem de constantes internas e ainda assim é enviado como parâmetro (sem concatenação de SQL).
        var value = await _context.Database.SqlQuery<long>($"SELECT nextval(CAST({sequence} AS regclass)) AS \"Value\"").SingleAsync(ct);
        return $"{prefix}{value:D6}";
    }
}

/// <summary>Sinal em memória para acordar o worker de notificações imediatamente (RNF11).</summary>
public sealed class NotificationDispatchSignal : INotificationDispatchSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void Signal()
    {
        if (_semaphore.CurrentCount == 0)
        {
            try { _semaphore.Release(); } catch (SemaphoreFullException) { /* já sinalizado */ }
        }
    }

    /// <summary>Aguarda um sinal ou o tempo máximo (o que ocorrer primeiro).</summary>
    public Task WaitAsync(TimeSpan timeout, CancellationToken ct) => _semaphore.WaitAsync(timeout, ct);
}
