using System.Net;
using FluentAssertions;
using Lacos.Application.Interfaces.Integrations;
using Lacos.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lacos.IntegrationTests;

/// <summary>
/// Busca de farmácias pela API HTTP (provedores externos substituídos por simulações, pois os testes não acessam a internet)
/// e inicialização da API antes da configuração do Supabase.
/// </summary>
public sealed class LocationAndStartupTests : ApiTestBase
{
    private HttpClient ClientWithFakeProviders(bool overpassDown = false)
    {
        var factory = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAddressLookupService>(new FakeAddress());
            services.AddSingleton<IGeocodingService>(new FakeGeocoding());
            services.AddSingleton<INearbyPlacesProvider>(new FakePlaces(overpassDown));
        }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, "auth0|farmacia");
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, "farmacia@lacos.test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailVerifiedHeader, "true");
        return client;
    }

    [Fact]
    public async Task Pharmacy_search_returns_sorted_results_and_maps_errors()
    {
        var client = ClientWithFakeProviders();
        await client.PostAsync("/api/auth/session", null);

        var ok = await client.GetAsync("/api/locations/pharmacies?cep=80010-000&radius=3000");
        await EnsureStatus(ok, HttpStatusCode.OK);
        var body = await Read(ok);
        body.GetProperty("city").GetString().Should().Be("Curitiba");
        body.GetProperty("origin").GetProperty("precision").GetString().Should().Be("STREET");
        var pharmacies = body.GetProperty("pharmacies");
        pharmacies.GetArrayLength().Should().Be(2);
        pharmacies[0].GetProperty("name").GetString().Should().Be("Farmácia Perto");
        pharmacies[0].GetProperty("googleMapsUrl").GetString().Should().StartWith("https://www.google.com/maps/search/?api=1&query=");

        (await ErrorCode(await client.GetAsync("/api/locations/pharmacies?cep=123"))).Should().Be("VALIDATION_FAILED");
        var notFound = await client.GetAsync("/api/locations/pharmacies?cep=99999-999");
        notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/locations/pharmacies?cep=80010000&radius=50000")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Pharmacy_provider_outage_returns_503()
    {
        var client = ClientWithFakeProviders(overpassDown: true);
        await client.PostAsync("/api/auth/session", null);
        var response = await client.GetAsync("/api/locations/pharmacies?cep=80010000");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await ErrorCode(response)).Should().Be("EXTERNAL_SERVICE_UNAVAILABLE");
    }

    [Fact]
    public async Task Api_starts_without_database_configuration_and_reports_it()
    {
        // fábrica "real" (Npgsql), sem connection string: simula a API antes da configuração do Supabase.
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("ConnectionStrings:Supabase", "");
            b.UseSetting("BackgroundJobs:Enabled", "false");
            b.UseSetting("Encryption:DataKey", Convert.ToBase64String(new byte[32]));
            b.UseSetting("Encryption:BlindIndexKey", Convert.ToBase64String(new byte[32]));
            b.UseSetting("Auth0:Domain", "test.local");
            b.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o =>
                {
                    o.DefaultScheme = o.DefaultAuthenticateScheme = o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                });
            });
        });
        var client = factory.CreateClient();

        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable); // Unhealthy
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, "auth0|semdb");
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, "semdb@lacos.test");
        var session = await client.PostAsync("/api/auth/session", null);
        session.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await ErrorCode(session)).Should().Be("DATABASE_NOT_CONFIGURED");
    }

    private sealed class FakeAddress : IAddressLookupService
    {
        public Task<AddressLookupResult?> LookupAsync(string cep, CancellationToken ct = default) =>
            Task.FromResult(cep == "99999999" ? null : new AddressLookupResult(cep, "Praça Tiradentes", "Centro", "Curitiba", "PR"));
    }

    private sealed class FakeGeocoding : IGeocodingService
    {
        public Task<GeoPoint?> GeocodeAsync(AddressLookupResult address, CancellationToken ct = default) =>
            Task.FromResult<GeoPoint?>(new GeoPoint(-25.4284, -49.2733, "STREET"));
    }

    private sealed class FakePlaces(bool down) : INearbyPlacesProvider
    {
        public string SourceName => "teste";
        public Task<IReadOnlyList<NearbyPlace>> SearchPharmaciesAsync(GeoPoint center, int radiusMeters, CancellationToken ct = default)
        {
            if (down) throw new Lacos.Domain.Exceptions.ExternalServiceException("Overpass", "indisponível");
            return Task.FromResult<IReadOnlyList<NearbyPlace>>(new[]
            {
                new NearbyPlace("osm:node/2", "Farmácia Longe", null, null, null, null, null, null, null, -25.4400, -49.2733),
                new NearbyPlace("osm:node/1", "Farmácia Perto", "Rua XV", "10", "Centro", "Curitiba", "PR", null, "(41) 3333-3333", -25.4290, -49.2733)
            });
        }
    }
}
