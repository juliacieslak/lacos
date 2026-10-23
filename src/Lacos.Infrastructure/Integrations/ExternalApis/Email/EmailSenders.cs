using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Integrations:Email";
    /// <summary>"SendGrid" ou "Log" (padrão: apenas registra em log).</summary>
    public string Provider { get; set; } = "Log";
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "nao-responda@lacos.app";
    public string FromName { get; set; } = "Laços";
}

/// <summary>Exemplo de implementação real: envio pela API v3 do SendGrid.</summary>
public sealed class SendGridEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly EmailOptions _options;

    public SendGridEmailSender(HttpClient http, IOptions<EmailOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
        {
            Content = JsonContent.Create(new
            {
                personalizations = new[] { new { to = new[] { new { email = toEmail, name = toName } } } },
                from = new { email = _options.FromEmail, name = _options.FromName },
                subject,
                content = new[] { new { type = "text/html", value = htmlBody } }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SendGridApiKey);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("SendGrid", $"falha no envio ({(int)response.StatusCode}).");
    }
}

/// <summary>Implementação de desenvolvimento: apenas registra o envio (sem conteúdo, que pode ter dados de saúde).</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[E-mail simulado] Para: {To} | Assunto: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
