using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Push;

public sealed class PushOptions
{
    public const string SectionName = "Integrations:Push";
    /// <summary>"OneSignal" ou "Log".</summary>
    public string Provider { get; set; } = "Log";
    public string OneSignalAppId { get; set; } = string.Empty;
    public string OneSignalApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Exemplo de implementação real: OneSignal. O front-end associa o dispositivo ao usuário com
/// <c>OneSignal.login(userId)</c> (external_id), por isso o back-end não precisa armazenar tokens de dispositivo.
/// </summary>
public sealed class OneSignalPushNotificationSender : IPushNotificationSender
{
    private readonly HttpClient _http;
    private readonly PushOptions _options;

    public OneSignalPushNotificationSender(HttpClient http, IOptions<PushOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task SendAsync(Guid userId, string title, string message, IDictionary<string, string>? data, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.onesignal.com/notifications")
        {
            Content = JsonContent.Create(new
            {
                app_id = _options.OneSignalAppId,
                include_aliases = new { external_id = new[] { userId.ToString() } },
                target_channel = "push",
                headings = new { pt = title, en = title },
                contents = new { pt = message, en = message },
                data
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Key", _options.OneSignalApiKey);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("OneSignal", $"falha no envio ({(int)response.StatusCode}).");
    }
}

/// <summary>Implementação de desenvolvimento: apenas registra o envio.</summary>
public sealed class LoggingPushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<LoggingPushNotificationSender> _logger;

    public LoggingPushNotificationSender(ILogger<LoggingPushNotificationSender> logger) => _logger = logger;

    public Task SendAsync(Guid userId, string title, string message, IDictionary<string, string>? data, CancellationToken ct = default)
    {
        _logger.LogInformation("[Push simulado] Usuário: {UserId} | Título: {Title}", userId, title);
        return Task.CompletedTask;
    }
}
