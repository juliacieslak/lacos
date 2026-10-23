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
| `ICidLookupService` | `WhoIcdCidLookupService`, `UnavailableCidLookupService` | `Integrations:WhoIcd` |
| `IIdentityProviderService` | `Auth0ManagementService` | `Auth0` |
| `IEmailSender` | `SendGridEmailSender`, `LoggingEmailSender` | `Integrations:Email` |
| `IPushNotificationSender` | `OneSignalPushNotificationSender`, `LoggingPushNotificationSender` | `Integrations:Push` |

## Como adicionar uma nova integração

1. Crie a interface em `Application/Interfaces/Integrations` (somente tipos do domínio/DTOs, nada de HTTP).
2. Crie uma pasta aqui com `XxxOptions` e `XxxService` (HttpClient tipado).
3. Registre em `DependencyInjection.AddExternalIntegrations` com `.AddStandardResilienceHandler()`.
4. Nunca registre dados sensíveis (CPF, diagnósticos) em logs ao chamar APIs externas.
