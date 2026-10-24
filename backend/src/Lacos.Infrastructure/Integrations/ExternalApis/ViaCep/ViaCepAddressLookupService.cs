using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Lacos.Infrastructure.Integrations.ExternalApis.Common;

namespace Lacos.Infrastructure.Integrations.ExternalApis.ViaCep;

public sealed class ViaCepOptions
{
    public const string SectionName = "Integrations:ViaCep";
    public string BaseUrl { get; set; } = "https://viacep.com.br/ws/";
}

/// <summary>
/// Consulta de CEP no ViaCEP (API pública gratuita) — mesma API do protótipo ConsultaCepApp.
/// Retorna null para CEP inexistente (o ViaCEP responde 200 com "erro": true).
/// Resultados ficam em cache por 24 h (endereços mudam raramente) e consultas simultâneas do mesmo CEP são unificadas.
/// Observação: o ViaCEP NÃO fornece coordenadas; elas vêm da geocodificação (Nominatim).
/// </summary>
public sealed class ViaCepAddressLookupService : IAddressLookupService
{
    private readonly HttpClient _http;
    private readonly CachedRequestCoalescer _cache;

    public ViaCepAddressLookupService(HttpClient http, CachedRequestCoalescer cache)
    {
        _http = http;
        _cache = cache;
    }

    public Task<AddressLookupResult?> LookupAsync(string cep, CancellationToken ct = default) =>
        _cache.GetOrAddAsync($"cep:{cep}", TimeSpan.FromHours(24), () => QueryAsync(cep, ct));

    private async Task<AddressLookupResult?> QueryAsync(string cep, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<ViaCepResponse>($"{cep}/json/", ct);
            return response is null || response.HasError || string.IsNullOrWhiteSpace(response.Localidade)
                ? null
                : new AddressLookupResult(cep, Empty(response.Logradouro), Empty(response.Bairro), response.Localidade!, response.Uf ?? string.Empty);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("ViaCEP", ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ExternalServiceException("ViaCEP", "tempo de resposta excedido.");
        }
    }

    // CEPs gerais de cidade retornam logradouro/bairro vazios.
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record ViaCepResponse(
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("localidade")] string? Localidade,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] JsonElement? Erro)
    {
        // O ViaCEP já retornou "erro" como booleano e como texto ("true"); ambos são tratados.
        public bool HasError => Erro is { } e && (e.ValueKind == JsonValueKind.True || (e.ValueKind == JsonValueKind.String && e.GetString() == "true"));
    }
}
