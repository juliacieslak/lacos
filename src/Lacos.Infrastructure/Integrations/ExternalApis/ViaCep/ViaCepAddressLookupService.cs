using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace Lacos.Infrastructure.Integrations.ExternalApis.ViaCep;

public sealed class ViaCepOptions
{
    public const string SectionName = "Integrations:ViaCep";
    public string BaseUrl { get; set; } = "https://viacep.com.br/ws/";
}

/// <summary>
/// Consulta de CEP no ViaCEP (API pública gratuita). Resultados ficam em cache por 24 h,
/// pois endereços mudam raramente e isso reduz latência e dependência do serviço externo.
/// </summary>
public sealed class ViaCepAddressLookupService : IAddressLookupService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public ViaCepAddressLookupService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<AddressLookupResult?> LookupAsync(string cep, CancellationToken ct = default)
    {
        if (_cache.TryGetValue($"cep:{cep}", out AddressLookupResult? cached)) return cached;
        try
        {
            var response = await _http.GetFromJsonAsync<ViaCepResponse>($"{cep}/json/", ct);
            var result = response is null || response.HasError || response.Localidade is null
                ? null
                : new AddressLookupResult(cep, response.Logradouro, response.Bairro, response.Localidade, response.Uf ?? string.Empty);
            _cache.Set($"cep:{cep}", result, TimeSpan.FromHours(24));
            return result;
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("ViaCEP", ex.Message);
        }
    }

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
