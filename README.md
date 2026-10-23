# Laços — Back-end (API)

API REST do **Laços**, plataforma de gestão de cuidados em que familiares, cuidadores e profissionais da saúde
acompanham pacientes (rotina, medicamentos, métricas de saúde, agenda, documentos e histórico clínico).

> Escopo: **somente back-end**. O front-end consumirá esta API (contratos JSON, documentados em Swagger).
> Análise completa dos requisitos, atores, fluxos e suposições: [`docs/ANALISE_DE_REQUISITOS.md`](docs/ANALISE_DE_REQUISITOS.md).

## Tecnologias

- C# / **.NET 8** / ASP.NET Core Web API
- **Entity Framework Core 8** + Npgsql → **PostgreSQL do Supabase**
- Auth0 (JWT) · FluentValidation · Serilog (logs estruturados JSON) · Swagger/OpenAPI
- xUnit, Moq, FluentAssertions (testes unitários e de integração)

## Arquitetura

Arquitetura em camadas (Clean Architecture), com dependências apontando para o domínio:

```
lacos-backend/
├── src/
│   ├── Lacos.Domain/            # Regras de negócio puras (sem dependências externas)
│   │   ├── Common/              # BaseEntity, PatientRecord, IAuditable, [SensitiveData]
│   │   ├── Entities/            # User, Patient, PatientUserPermission, HealthThreshold, PatientEvent, ...
│   │   ├── Enums/  ValueObjects/ (Cpf, Cep, CidCode, UnitConverter)  Exceptions/
│   │   └── Interfaces/          # Contratos dos repositórios e Unit of Work
│   ├── Lacos.Application/       # Casos de uso
│   │   ├── DTOs/  Mappings/  Validators/ (FluentValidation)  Options/
│   │   ├── Interfaces/          # ICurrentUser, criptografia, integrações externas
│   │   └── Services/            # Serviços de aplicação (Service Layer) + Clinical/
│   ├── Lacos.Infrastructure/    # Detalhes técnicos
│   │   ├── Persistence/         # DbContext, Configurations, Repositories, Interceptors, Migrations
│   │   ├── Security/            # AES-256-GCM, blind index HMAC
│   │   ├── Integrations/ExternalApis/  # Supabase Storage, ViaCEP, OMS (CID), Auth0, SendGrid, OneSignal
│   │   ├── BackgroundJobs/      # Lembretes e envio de notificações
│   │   └── Services/            # Relógio/fuso, gerador de códigos U/P
│   └── Lacos.Api/               # Apresentação HTTP
│       ├── Controllers/  Middlewares/  Filters/  ModelBinding/  Security/  Extensions/
│       └── Program.cs
├── tests/
│   ├── Lacos.UnitTests/         # Domínio, segurança e serviços
│   └── Lacos.IntegrationTests/  # API completa (InMemory ou PostgreSQL real)
├── database/schema.sql          # Script SQL idempotente gerado das migrations (para o SQL Editor do Supabase)
└── docs/ANALISE_DE_REQUISITOS.md
```

Padrões aplicados: SOLID, DRY, Repository + Unit of Work, Service Layer, DTO, Entity Configuration,
Template Method (`PatientRecordService` / `PatientRecordControllerBase` para todos os registros clínicos),
Dependency Injection, tratamento global de exceções (ProblemDetails com `code`) e logging estruturado.

## Como executar

### Pré-requisitos

- .NET SDK 8 · PostgreSQL (Supabase ou local) · VS Code com a extensão **C# Dev Kit**

### 1. Configurar segredos (nunca versione chaves reais)

```bash
cd src/Lacos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=db.<projeto>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<senha>;SSL Mode=Require"
dotnet user-secrets set "Encryption:DataKey" "$(openssl rand -base64 32)"
dotnet user-secrets set "Encryption:BlindIndexKey" "$(openssl rand -base64 32)"
dotnet user-secrets set "Auth0:Domain" "<tenant>.us.auth0.com"
dotnet user-secrets set "Auth0:Audience" "https://api.lacos.app"
```

Em produção, use variáveis de ambiente (ex.: `Encryption__DataKey`, `ConnectionStrings__Supabase`).
**Atenção:** perder a `Encryption:DataKey` torna os dados criptografados irrecuperáveis — guarde-a em um cofre de segredos.
O `appsettings.Development.json` contém chaves **apenas para desenvolvimento local**.

### 2. Banco de dados

Opção A — migrations (recomendado):

```bash
dotnet tool restore
LACOS_MIGRATIONS_CONNECTION="<connection string>" dotnet ef database update -p src/Lacos.Infrastructure -s src/Lacos.Infrastructure
```

Opção B — Supabase SQL Editor: execute `database/schema.sql`.

Em desenvolvimento, `Database:ApplyMigrationsOnStartup=true` aplica as migrations ao iniciar.

### 3. Executar

```bash
dotnet run --project src/Lacos.Api
```

Swagger (desenvolvimento): `https://localhost:<porta>/swagger` · Health check: `/health`.
No VS Code: **F5** (configuração "Laços API") ou a tarefa `build`.

### 4. Testes

```bash
dotnet test                                     # unitários + integração (EF InMemory)
LACOS_TEST_POSTGRES="Host=localhost;Database=postgres;Username=postgres;Password=postgres" dotnet test
                                                # integração contra PostgreSQL real (migrations, triggers, sequências)
```

## Configuração do Auth0

1. Crie uma **API** (Identifier = `Auth0:Audience`) e uma **Single Page Application** para o front-end.
2. Configure o *Inactivity timeout* do tenant para 15 minutos (RNF03, complementar ao middleware da API).
3. Crie uma **Action** (Login flow) que adicione e-mail e nome ao access token:

```js
exports.onExecutePostLogin = async (event, api) => {
  const ns = 'https://lacos.app/';
  api.accessToken.setCustomClaim(ns + 'email', event.user.email);
  api.accessToken.setCustomClaim(ns + 'email_verified', event.user.email_verified);
  api.accessToken.setCustomClaim(ns + 'name', event.user.name);
};
```

4. (Opcional) Crie uma aplicação **Machine to Machine** com permissão `delete:users` na Management API e
   preencha `Auth0:ManagementClientId/Secret` para a exclusão de conta remover o usuário do Auth0.

## Configuração do Supabase

- Banco: use a connection string (porta 5432 direta ou 6543 do pooler) com `SSL Mode=Require`.
- Storage: crie um bucket **privado** `patient-documents` e configure `Integrations:Storage` (`Provider=Supabase`, URL, `ServiceRoleKey`).
- A migration `DatabaseSecurity` habilita **RLS** em todas as tabelas e revoga privilégios dos papéis `anon` e
  `authenticated`: os dados **não** ficam expostos pela API REST pública do Supabase — somente este back-end os acessa.
- Backups (RNF10): habilite backups diários / Point-in-Time Recovery no plano do Supabase.

## Fluxo de uso da API (front-end)

1. Login no Auth0 → `POST /api/auth/session` → `accessStatus`: `AUTHORIZED` (Dashboard), `PENDING_APPROVAL` (convites pendentes) ou `NO_INVITATION`.
2. Primeiro acesso: `POST /api/users/me/onboarding` (dados do RF01 + aceite LGPD).
3. `POST /api/patients` (vira Administrador do Perfil) ou `POST /api/invitations/{id}/accept`.
4. Uso diário: `GET /api/dashboard`, `GET /api/agenda`, registros clínicos, `GET /api/notifications`.

Erros seguem RFC 7807 com `code` estável (ex.: `ONBOARDING_REQUIRED`, `CONSENT_REQUIRED`, `PERMISSION_DENIED`,
`SESSION_EXPIRED`, `LAST_MANAGER`, `VALIDATION_FAILED` com `errors` por campo). Enums trafegam em `UPPER_SNAKE_CASE`
(iguais aos valores do banco), inclusive em query strings.

## Endpoints

| Recurso | Rotas |
|---|---|
| Sessão | `POST /api/auth/session`, `POST /api/auth/logout` |
| Usuário | `GET/PUT/DELETE /api/users/me`, `POST /api/users/me/onboarding`, `GET/POST /api/users/me/consents`, `POST /api/users/me/consents/revoke` |
| Pacientes | `GET/POST /api/patients`, `GET/PUT/DELETE /api/patients/{id}`, `PATCH .../status`, `PUT .../measurement-units` |
| Rede de cuidado | `GET .../members`, `PUT/DELETE .../members/{permissionId}`, `GET/POST .../invitations`, `DELETE .../invitations/{id}`, `GET /api/invitations/mine`, `POST /api/invitations/{id}/accept\|decline` |
| Registros clínicos (CRUD + lista paginada) | `.../medications` (+`current`, `discontinue`), `.../medication-administrations`, `.../measurements`, `.../symptoms`, `.../care-diary`, `.../health-conditions`, `.../surgeries`, `.../allergies`, `.../events` (+`range`, `status`, `summary`), `.../care-routines` (+`completions`) |
| Limites | `GET/PUT .../health-thresholds` |
| Documentos | `POST/GET .../documents`, `GET/PUT/DELETE .../documents/{id}`, `GET .../documents/{id}/content` |
| Visões | `GET /api/dashboard`, `GET /api/agenda`, `GET .../timeline`, `GET .../history` |
| Notificações | `GET /api/notifications`, `POST .../{id}/read`, `POST .../read-all`, `GET /api/notifications/preferences`, `PUT .../preferences/{patientId}` |
| Apoio | `GET /api/lookups/cep/{cep}`, `GET /api/lookups/cid?term=` |

(`...` = `/api/patients/{patientId}`)

## Segurança

- **Autenticação** JWT do Auth0 (assinatura, emissor, audiência, expiração); todas as rotas exigem autenticação por padrão.
- **Autorização por paciente** em toda operação (consultar/adicionar/alterar/excluir/gerenciar membros), lida do banco a cada requisição → revogação imediata; 404 para quem não tem vínculo (evita enumeração).
- **LGPD**: consentimentos versionados com IP; bloqueio sem consentimento vigente; CPF mascarado nas respostas; exclusão definitiva de conta e de paciente.
- **Criptografia**: AES-256-GCM para dados sensíveis (atributo `[SensitiveData]`) e anexos; blind index HMAC-SHA256 para unicidade do CPF; TLS/HSTS.
- **Auditoria imutável** com IP, User-Agent e visualizações; trigger no banco impede alterações.
- **Sessão**: timeout por inatividade de 15 min e logout com revogação do token.
- **Entrada**: FluentValidation, validação de arquivos por assinatura binária, sanitização de nomes, limites de tamanho.
- **Proteções HTTP**: rate limiting (global, uploads, operações sensíveis), CORS restrito, cabeçalhos de segurança, `Cache-Control: no-store`, erros sem detalhes internos.
- **Banco**: RLS + revogação dos papéis públicos do Supabase; FKs `RESTRICT`; índices únicos.

## Integrações externas

Camada desacoplada em `src/Lacos.Infrastructure/Integrations/ExternalApis` (ver o [README da pasta](src/Lacos.Infrastructure/Integrations/ExternalApis/README.md)):
Supabase Storage, ViaCEP, API ICD da OMS (CID), Auth0 Management, SendGrid (e-mail) e OneSignal (push) — todas atrás de
interfaces da camada de aplicação, com HttpClient tipado e resiliência (retry, circuit breaker, timeout) e fallback seguro.

## Escalabilidade e próximos passos

- Para várias instâncias: trocar `AddDistributedMemoryCache` por Redis (sessões/revogações) e manter os workers em uma
  única instância ou usar *leader election*; a deduplicação das notificações já é garantida por índice único.
- Linha do tempo muito grande: substituir o merge em memória por uma *view* SQL (`UNION ALL`) sem alterar o contrato.
- Rotação de chaves: o formato `enc:v1:` permite introduzir `v2` mantendo a leitura dos dados antigos.
