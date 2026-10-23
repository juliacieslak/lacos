using Lacos.Application.Services;
using Lacos.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.BackgroundJobs;

/// <summary>Configuração dos trabalhos em segundo plano.</summary>
public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";
    public bool Enabled { get; set; } = true;
    /// <summary>Intervalo da verificação de lembretes (padrão: 60 s).</summary>
    public int ReminderIntervalSeconds { get; set; } = 60;
    /// <summary>Intervalo máximo entre rodadas de envio (RNF11: entrega em até 1 minuto; padrão 15 s).</summary>
    public int DispatchIntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Worker que executa periodicamente a geração de lembretes (medicação, compromissos, rotina) e alertas de dose não registrada.
/// Cria um escopo de DI por rodada (DbContext novo) e nunca derruba o processo em caso de erro.
/// </summary>
public sealed class CareReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<CareReminderWorker> _logger;

    public CareReminderWorker(IServiceScopeFactory scopeFactory, IOptions<BackgroundJobOptions> options, ILogger<CareReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(10, _options.ReminderIntervalSeconds)));
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ICareReminderService>().RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro na rotina de lembretes");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>
/// Worker de envio das notificações pendentes (RNF11). Acorda a cada intervalo ou imediatamente quando
/// uma nova notificação é enfileirada (sinal em memória).
/// </summary>
public sealed class NotificationDispatchWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationDispatchSignal _signal;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<NotificationDispatchWorker> _logger;

    public NotificationDispatchWorker(IServiceScopeFactory scopeFactory, NotificationDispatchSignal signal,
        IOptions<BackgroundJobOptions> options, ILogger<NotificationDispatchWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = signal;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(_options.DispatchIntervalSeconds, 1, 60));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<INotificationDispatchService>().DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro no envio de notificações");
            }

            try { await _signal.WaitAsync(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
