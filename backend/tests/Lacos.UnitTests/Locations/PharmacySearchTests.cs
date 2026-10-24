using System.Net;
using System.Text;
using FluentAssertions;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Application.Services;
using Lacos.Domain.Exceptions;
using Lacos.Infrastructure.Integrations.ExternalApis.Common;
using Lacos.Infrastructure.Integrations.ExternalApis.Nominatim;
using Lacos.Infrastructure.Integrations.ExternalApis.Overpass;
using Lacos.Infrastructure.Integrations.ExternalApis.ViaCep;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Lacos.UnitTests.Locations;

/// <summary>
/// Testes da busca de farmácias por CEP. As APIs externas são simuladas com respostas no formato real
/// documentado por cada provedor (ViaCEP, Nominatim e Overpass); nenhum acesso à internet é feito.
/// </summary>
public class PharmacySearchTests
{
    private static readonly GeoPoint Origin = new(-25.4284, -49.2733, "STREET");

    // ---------- Regras do serviço ----------

    [Theory]
    [InlineData("123")]
    [InlineData("abcde-fgh")]
    [InlineData("00000-000")]
    public async Task Invalid_cep_is_rejected_without_calling_external_apis(string cep)
    {
        var address = new Mock<IAddressLookupService>(MockBehavior.Strict);
        var sut = new PharmacySearchService(address.Object, Mock.Of<IGeocodingService>(), Mock.Of<INearbyPlacesProvider>());
        var act = () => sut.SearchByCepAsync(cep, null);
        await act.Should().ThrowAsync<ValidationFailedException>();
    }

    [Fact]
    public async Task Nonexistent_cep_returns_not_found()
    {
        var address = new Mock<IAddressLookupService>();
        address.Setup(a => a.LookupAsync("99999999", It.IsAny<CancellationToken>())).ReturnsAsync((AddressLookupResult?)null);
        var sut = new PharmacySearchService(address.Object, Mock.Of<IGeocodingService>(), Mock.Of<INearbyPlacesProvider>());
        var act = () => sut.SearchByCepAsync("99999-999", null);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Results_are_deduplicated_sorted_by_distance_and_limited_to_radius()
    {
        var address = new Mock<IAddressLookupService>();
        address.Setup(a => a.LookupAsync("80010000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddressLookupResult("80010000", "Praça Tiradentes", "Centro", "Curitiba", "PR"));
        var geocoding = new Mock<IGeocodingService>();
        geocoding.Setup(g => g.GeocodeAsync(It.IsAny<AddressLookupResult>(), It.IsAny<CancellationToken>())).ReturnsAsync(Origin);
        var places = new Mock<INearbyPlacesProvider>();
        places.SetupGet(p => p.SourceName).Returns("teste");
        places.Setup(p => p.SearchPharmaciesAsync(Origin, 3000, It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            Place("osm:node/1", "Farmácia Longe", -25.4450, -49.2733),             // ~1,8 km
            Place("osm:node/2", "Farmácia Perto", -25.4290, -49.2733),             // ~70 m
            Place("osm:way/3", "farmácia perto", -25.4291, -49.2734, phone: "+55 41 3333-3333"), // mesmo local, mais completo
            Place("osm:node/2", "Farmácia Perto", -25.4290, -49.2733),             // mesmo id repetido
            Place("osm:node/4", null, -25.4300, -49.2733),                           // sem nome cadastrado
            Place("osm:node/5", "Fora do raio", -25.4700, -49.2733)                 // ~4,6 km
        });

        var result = await new PharmacySearchService(address.Object, geocoding.Object, places.Object).SearchByCepAsync("80010-000", null);

        result.Cep.Should().Be("80010-000");
        result.City.Should().Be("Curitiba");
        result.Pharmacies.Select(p => p.Id).Should().Equal("osm:way/3", "osm:node/4", "osm:node/1");
        result.Pharmacies[0].Phone.Should().Be("+55 41 3333-3333");
        result.Pharmacies[0].GoogleMapsUrl.Should().Be("https://www.google.com/maps/search/?api=1&query=-25.4291,-49.2734");
        result.Pharmacies[1].HasRegisteredName.Should().BeFalse();
        result.Pharmacies.Should().BeInAscendingOrder(p => p.DistanceMeters);
        result.Pharmacies[2].DistanceMeters.Should().BeInRange(1800, 1900);
    }

    [Fact]
    public async Task Geocoding_failure_is_reported()
    {
        var address = new Mock<IAddressLookupService>();
        address.Setup(a => a.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddressLookupResult("80010000", null, null, "Curitiba", "PR"));
        var sut = new PharmacySearchService(address.Object, Mock.Of<IGeocodingService>(), Mock.Of<INearbyPlacesProvider>());
        var act = () => sut.SearchByCepAsync("80010000", 3000);
        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be("GEOCODING_FAILED");
    }

    [Fact]
    public void Haversine_matches_known_distance()
    {
        // Praça da Sé (SP) → Pça. Tiradentes (Curitiba): ~339 km.
        PharmacySearchService.Haversine(-23.5505, -46.6333, -25.4284, -49.2733).Should().BeInRange(335_000, 345_000);
    }

    // ---------- Integrações (HTTP simulado) ----------

    [Fact]
    public async Task ViaCep_maps_address_and_treats_erro_as_nonexistent()
    {
        var handler = new FakeHandler(req => req.RequestUri!.AbsolutePath.Contains("99999999")
            ? Json("""{"erro": "true"}""")
            : Json("""{"cep":"80010-000","logradouro":"Praça Tiradentes","bairro":"Centro","localidade":"Curitiba","uf":"PR"}"""));
        var sut = new ViaCepAddressLookupService(new HttpClient(handler) { BaseAddress = new Uri("https://viacep.test/ws/") }, Coalescer());

        (await sut.LookupAsync("80010000"))!.City.Should().Be("Curitiba");
        (await sut.LookupAsync("99999999")).Should().BeNull();
    }

    [Fact]
    public async Task ViaCep_unavailable_becomes_external_service_error()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("falha de rede"));
        var sut = new ViaCepAddressLookupService(new HttpClient(handler) { BaseAddress = new Uri("https://viacep.test/ws/") }, Coalescer());
        var act = () => sut.LookupAsync("80010000");
        await act.Should().ThrowAsync<ExternalServiceException>();
    }

    [Fact]
    public async Task Simultaneous_identical_requests_call_the_api_once()
    {
        var calls = 0;
        var handler = new FakeHandler(_ =>
        {
            Interlocked.Increment(ref calls);
            Thread.Sleep(100);
            return Json("""{"cep":"80010-000","logradouro":"","bairro":"","localidade":"Curitiba","uf":"PR"}""");
        });
        var sut = new ViaCepAddressLookupService(new HttpClient(handler) { BaseAddress = new Uri("https://viacep.test/ws/") }, Coalescer());

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => sut.LookupAsync("80010000")));
        results.Should().OnlyContain(r => r != null && r.Street == null);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Nominatim_falls_back_from_street_to_postal_code()
    {
        var requested = new List<string>();
        var handler = new FakeHandler(req =>
        {
            requested.Add(Uri.UnescapeDataString(req.RequestUri!.Query));
            return req.RequestUri!.Query.Contains("street=")
                ? Json("[]")
                : Json("""[{"lat":"-25.4290","lon":"-49.2710","display_name":"Curitiba"}]""");
        });
        var sut = new NominatimGeocodingService(new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") }, Coalescer(),
            Options.Create(new NominatimOptions { MinIntervalMilliseconds = 0 }), NullLogger<NominatimGeocodingService>.Instance);

        var point = await sut.GeocodeAsync(new AddressLookupResult("80010000", "Praça Tiradentes", "Centro", "Curitiba", "PR"));

        point.Should().Be(new GeoPoint(-25.4290, -49.2710, "POSTAL_CODE"));
        requested.Should().HaveCount(2);
        requested[1].Should().Contain("postalcode=80010-000").And.Contain("countrycodes=br");
    }

    [Fact]
    public async Task Overpass_maps_nodes_and_ways_and_fails_over_between_endpoints()
    {
        var hosts = new List<string>();
        var handler = new FakeHandler(req =>
        {
            hosts.Add(req.RequestUri!.Host);
            if (req.RequestUri.Host == "primary.test") return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
            return Json("""
                {"elements":[
                  {"type":"node","id":10,"lat":-25.43,"lon":-49.27,"tags":{"amenity":"pharmacy","name":"Drogaria A","phone":"(41) 3000-0000","addr:street":"Rua XV de Novembro","addr:housenumber":"100","addr:city":"Curitiba"}},
                  {"type":"way","id":20,"center":{"lat":-25.431,"lon":-49.271},"tags":{"healthcare":"pharmacy","brand":"Rede B"}},
                  {"type":"relation","id":30,"tags":{"amenity":"pharmacy","name":"Sem coordenada"}}
                ]}
                """);
        });
        var sut = new OverpassNearbyPlacesProvider(new HttpClient(handler), Coalescer(),
            Options.Create(new OverpassOptions { Endpoints = new[] { "https://primary.test/api/interpreter", "https://mirror.test/api/interpreter" } }),
            NullLogger<OverpassNearbyPlacesProvider>.Instance);

        var places = await sut.SearchPharmaciesAsync(Origin, 3000);

        hosts.Should().Equal("primary.test", "mirror.test");
        places.Should().HaveCount(2);
        places[0].Should().Be(new NearbyPlace("osm:node/10", "Drogaria A", "Rua XV de Novembro", "100", null, "Curitiba", null, null,
            "(41) 3000-0000", -25.43, -49.27));
        places[1].Name.Should().Be("Rede B");
        places[1].Latitude.Should().Be(-25.431);
    }

    [Fact]
    public async Task Overpass_all_endpoints_down_becomes_external_service_error()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var sut = new OverpassNearbyPlacesProvider(new HttpClient(handler), Coalescer(),
            Options.Create(new OverpassOptions { Endpoints = new[] { "https://a.test/i", "https://b.test/i" } }),
            NullLogger<OverpassNearbyPlacesProvider>.Instance);
        var act = () => sut.SearchPharmaciesAsync(Origin, 3000);
        await act.Should().ThrowAsync<ExternalServiceException>();
    }

    private static NearbyPlace Place(string id, string? name, double lat, double lon, string? phone = null) =>
        new(id, name, null, null, null, null, null, null, phone, lat, lon);

    private static CachedRequestCoalescer Coalescer() => new(new MemoryCache(new MemoryCacheOptions()));

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.Run(() => _respond(request), cancellationToken);
    }
}
