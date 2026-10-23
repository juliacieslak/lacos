using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace Lacos.IntegrationTests.Infrastructure;

/// <summary>Base dos testes de integração: fábrica isolada por classe de teste e utilitários HTTP/JSON.</summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    protected LacosApiFactory Factory { get; } = new();

    public Task InitializeAsync() => Factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Cria um usuário com sessão iniciada e onboarding concluído.</summary>
    protected async Task<HttpClient> RegisteredUserAsync(string sub, string email, string cpf, string name = "Usuário Teste",
        string profileType = "FAMILY")
    {
        var client = Factory.CreateClientFor(sub, email, name: name);
        (await client.PostAsync("/api/auth/session", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var onboarding = await client.PostAsJsonAsync("/api/users/me/onboarding", new
        {
            profileType, name, cpf, birthDate = "1985-04-12", phone = "(41) 99999-0000", profession = "Analista",
            cep = "80010-000", ethnicity = "PARDA", gender = "FEMALE", acceptTermsOfUse = true, acceptSensitiveDataProcessing = true
        }, Json);
        await EnsureStatus(onboarding, HttpStatusCode.OK);
        return client;
    }

    /// <summary>Cria um paciente e retorna seu id.</summary>
    protected static async Task<JsonElement> CreatePatientAsync(HttpClient client, string cpf, string name = "Maria Silva")
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            fullName = name, birthDate = "1950-03-10", birthPlace = "Curitiba/PR", cep = "80010000", ethnicity = "BRANCA", cpf,
            gender = "FEMALE", email = (string?)null, phone = (string?)null, heightCm = 160, relationshipType = "filha"
        }, Json);
        await EnsureStatus(response, HttpStatusCode.Created);
        return await Read(response);
    }

    protected static async Task<JsonElement> Read(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    protected static async Task EnsureStatus(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
            throw new Xunit.Sdk.XunitException($"Esperado {(int)expected}, obtido {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    protected static async Task<string> ErrorCode(HttpResponseMessage response) =>
        (await Read(response)).GetProperty("code").GetString()!;
}
