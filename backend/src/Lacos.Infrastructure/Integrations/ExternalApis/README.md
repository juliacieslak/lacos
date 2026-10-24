# Integrações externas (pontos de extensão)

Toda integração segue o mesmo padrão, para que novas APIs possam ser adicionadas sem alterar regras de negócio:

1. **Contrato** na camada de aplicação: `Lacos.Application/Interfaces/Integrations/IExternalIntegrations.cs`.
2. **Opções** (`*Options`) lidas do `appsettings.json` / variáveis de ambiente (seção `Integrations:*`).
3. **Implementação** nesta pasta, usando `HttpClient` tipado com resiliência padrão
   (retry com backoff exponencial, circuit breaker e timeout — `Microsoft.Extensions.Http.Resilience`).
4. **Fallback** seguro quando a integração não está configurada (ex.: e-mails apenas registrados em log em desenvolvimento).
5. **Registro** em `Infrastructure/DependencyInjection.cs`, escolhido por configuração (`Provider`).

| Contrato | Implementações | Configuração |
|---|---|---|
| `IFileStorage` | `SupabaseFileStorage`, `LocalFileStorage` | `Integrations:Storage` |
| `IAddressLookupService` | `ViaCepAddressLookupService` | `Integrations:ViaCep` |
| `IGeocodingService` | `NominatimGeocodingService` (OpenStreetMap) | `Integrations:Nominatim` |
| `INearbyPlacesProvider` | `OverpassNearbyPlacesProvider` (OpenStreetMap) | `Integrations:Overpass` |
| `ICidLookupService` | `WhoIcdCidLookupService`, `UnavailableCidLookupService` | `Integrations:WhoIcd` |
| `IIdentityProviderService` | `Auth0ManagementService` | `Auth0` |
| `IEmailSender` | `SendGridEmailSender`, `LoggingEmailSender` | `Integrations:Email` |
| `IPushNotificationSender` | `OneSignalPushNotificationSender`, `LoggingPushNotificationSender` | `Integrations:Push` |

## Busca de farmácias por CEP

Nenhuma API isolada oferece tudo: o ViaCEP valida o CEP e devolve o endereço (sem coordenadas); o Nominatim converte
o endereço em coordenadas; a Overpass API lista as farmácias do OpenStreetMap no raio. O `PharmacySearchService`
(camada de aplicação) orquestra as três, remove duplicidades, calcula a distância (Haversine) e ordena.
Para trocar de provedor (ex.: Google Places), basta implementar `IGeocodingService`/`INearbyPlacesProvider` e
registrar a nova classe — a chave de API ficaria apenas no backend.

Limites de uso respeitados: cache (CEP/coordenadas 24 h, farmácias 10 min), coalescência de requisições idênticas,
no máximo 1 requisição/s ao Nominatim com `User-Agent` identificado, timeouts e failover entre instâncias Overpass,
além de limite de 20 buscas/minuto por usuário na API.

## Como adicionar uma nova integração

1. Crie a interface em `Application/Interfaces/Integrations` (somente tipos do domínio/DTOs, nada de HTTP).
2. Crie uma pasta aqui com `XxxOptions` e `XxxService` (HttpClient tipado).
3. Registre em `DependencyInjection.AddExternalIntegrations` com `.AddStandardResilienceHandler()`.
4. Nunca registre dados sensíveis (CPF, diagnósticos) em logs ao chamar APIs externas.
