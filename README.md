# Laços — cuidado conectado

Plataforma em que familiares, cuidadores e profissionais da saúde organizam o cuidado de pacientes:
rotina, medicamentos, medições, agenda, documentos, histórico clínico, rede de apoio e farmácias próximas.

```
Frontend React (Vite)  ──HTTP + token Auth0──▶  Backend .NET 8 (API REST)  ──▶  Supabase (PostgreSQL)
                                                        │
                                                        └──▶ ViaCEP · Nominatim · Overpass (OpenStreetMap)
```

| Pasta | Conteúdo |
|---|---|
| `frontend/` | SPA em **TypeScript + React 19 + Vite + Tailwind CSS 4 + React Router 7 + Auth0 React SDK + Lucide** |
| `backend/` | API em **C# / .NET 8 / ASP.NET Core / EF Core 8 (Npgsql)** em camadas (Domain, Application, Infrastructure, Api) + testes |

---

## 1. Executar localmente

### Pré-requisitos
- .NET SDK 8 · Node.js 20+ · VS Code (extensões recomendadas em `.vscode/extensions.json`)

### Backend (porta 5080)
```bash
cd backend
dotnet restore
dotnet run --project src/Lacos.Api --launch-profile http     # Swagger: http://localhost:5080/swagger
dotnet test                                                  # testes unitários e de integração
```
No VS Code: **F5 → "Laços API (backend)"**.

A API **inicia mesmo sem o Supabase configurado**: `/health` informa `Unhealthy` e as rotas que dependem do banco respondem
`503 DATABASE_NOT_CONFIGURED` até a connection string ser definida.

### Frontend (porta 5173)
```bash
cd frontend
cp .env.example .env.local    # preencha as variáveis (ver seção 3)
npm install
npm run dev                   # http://localhost:5173
npm run lint && npm run build # verificação de lint, tipos e build de produção
```
Sem as variáveis do Auth0, o frontend abre uma tela indicando exatamente o que falta configurar.

---

## 2. Configurar o Supabase (estrutura criada manualmente)

A aplicação **não cria nem altera** tabelas, schemas ou sequências: não há migrations, `EnsureCreated` ou scripts SQL
de inicialização. A estrutura é criada por você diretamente no Supabase; o backend apenas se conecta.

1. **Connection string** (somente no backend, nunca no frontend) — em *Project Settings → Database*:
   ```bash
   cd backend/src/Lacos.Api
   dotnet user-secrets set "ConnectionStrings:Supabase" "Host=db.<projeto>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<senha>;SSL Mode=Require"
   ```
   Em produção use a variável de ambiente `ConnectionStrings__Supabase`.
2. **Tabelas esperadas** (nomes em `snake_case`): `users`, `user_consents`, `patients`, `patient_user_permissions`,
   `patient_invitations`, `health_thresholds`, `patient_events`, `notification_preferences`, `notifications`, `audit_logs`,
   `medications`, `medication_administrations`, `health_measurements`, `symptom_records`, `care_diary_entries`,
   `care_routine_tasks`, `care_routine_completions`, `health_conditions`, `surgeries`, `allergies`, `patient_documents`.
   As colunas, tipos, índices e chaves de cada tabela estão declarados em
   `backend/src/Lacos.Infrastructure/Persistence/Configurations/` (um arquivo por grupo de entidades).
3. **Sequências** usadas nos códigos públicos (RF17 — `U000001`, `P000001`): `user_public_code_seq` e `patient_public_code_seq`.
4. **Storage**: crie um bucket **privado** `patient-documents` e configure `Integrations:Storage` (`Provider=Supabase`,
   `SupabaseUrl`, `ServiceRoleKey` via user-secrets/variável de ambiente). Em desenvolvimento o padrão é disco local.
5. **Recomendações de segurança no banco** (feitas por você no Supabase):
   - habilitar *Row Level Security* em todas as tabelas e revogar privilégios dos papéis `anon` e `authenticated`,
     para que os dados não fiquem expostos pela API REST pública do Supabase (somente o backend acessa);
   - opcionalmente, uma *trigger* que impeça `UPDATE/DELETE` em `audit_logs` e a função `redact_patient_audit_values(uuid)`
     usada na exclusão definitiva (LGPD). Se a função não existir, a exclusão continua e o fato é registrado em log.
6. Backups diários / Point-in-Time Recovery no plano do Supabase (RNF10).

---

## 3. Variáveis de ambiente

### Frontend — `frontend/.env.local` (somente valores públicos)
| Variável | Exemplo |
|---|---|
| `VITE_AUTH0_DOMAIN` | `seu-tenant.us.auth0.com` |
| `VITE_AUTH0_CLIENT_ID` | Client ID da aplicação SPA no Auth0 |
| `VITE_AUTH0_AUDIENCE` | `https://api.lacos.app` (igual ao backend) |
| `VITE_API_URL` | `http://localhost:5080` |

### Backend — `dotnet user-secrets` ou variáveis de ambiente (`Secao__Chave`)
| Chave | Obrigatória | Observação |
|---|---|---|
| `ConnectionStrings:Supabase` | para usar o banco | PostgreSQL do Supabase com `SSL Mode=Require` |
| `Auth0:Domain` / `Auth0:Audience` | sim | validação dos access tokens |
| `Auth0:ClaimsNamespace` | não | padrão `https://lacos.app/` (ver Action abaixo) |
| `Auth0:ManagementClientId/Secret` | não | remover a conta no Auth0 ao excluir a conta (LGPD) |
| `Encryption:DataKey` / `Encryption:BlindIndexKey` | sim (produção) | `openssl rand -base64 32` cada. Perder a DataKey torna os dados criptografados irrecuperáveis. O `appsettings.Development.json` traz chaves **apenas para desenvolvimento** |
| `Integrations:Storage:*` | não | `Provider=Supabase`, `SupabaseUrl`, `ServiceRoleKey`, `Bucket` |
| `Integrations:Nominatim:UserAgent` | recomendado | identifique a aplicação com um e-mail de contato (política de uso do Nominatim) |
| `Integrations:Email:*`, `Integrations:Push:*` | não | SendGrid / OneSignal; padrão: apenas registra em log |
| `Integrations:WhoIcd:*` | não | busca de códigos CID na API da OMS |
| `Cors:AllowedOrigins` | sim (produção) | em desenvolvimento já inclui `http://localhost:5173` |

---

## 4. Configurar o Auth0

1. **APIs → Create API**: *Identifier* = `https://api.lacos.app` (use o mesmo valor em `Auth0:Audience` e `VITE_AUTH0_AUDIENCE`).
   Em *Settings*, habilite **Allow Offline Access** (refresh tokens).
2. **Applications → Create Application → Single Page Application** (fluxo Authorization Code + PKCE; não há client secret no frontend):
   - *Allowed Callback URLs*: `http://localhost:5173`
   - *Allowed Logout URLs*: `http://localhost:5173/login`
   - *Allowed Web Origins*: `http://localhost:5173`
   - *Refresh Token Rotation*: **habilitado** (o SDK usa refresh tokens para manter a sessão ao recarregar a página)
   - Adicione as URLs de produção quando houver.
3. **Actions → Library → Build Custom (Login / Post Login)** — inclui e-mail e nome no access token (o backend registra o usuário com eles):
   ```js
   exports.onExecutePostLogin = async (event, api) => {
     const ns = 'https://lacos.app/';
     api.accessToken.setCustomClaim(ns + 'email', event.user.email);
     api.accessToken.setCustomClaim(ns + 'email_verified', event.user.email_verified);
     api.accessToken.setCustomClaim(ns + 'name', event.user.name);
   };
   ```
   Adicione a Action ao fluxo *Login*.
4. *Tenant Settings → Advanced*: *Inactivity timeout* = 15 minutos (o backend e o frontend também encerram a sessão após 15 min sem uso — RNF03).
5. (Opcional) Aplicação **Machine to Machine** com a permissão `delete:users` na Management API para a exclusão de conta.

Convites da rede de cuidado só podem ser aceitos com **e-mail verificado** no Auth0.

---

## 5. Busca de farmácias por CEP

`GET /api/locations/pharmacies?cep=80010-000&radius=3000` (autenticado; o frontend nunca chama as APIs externas diretamente).

1. **Validação** do CEP (8 dígitos) no frontend e no backend.
2. **ViaCEP** — confirma que o CEP existe (CEP inexistente → 404) e devolve logradouro, bairro, cidade e UF. *Não fornece coordenadas.*
3. **Nominatim (OpenStreetMap)** — geocodificação: tenta logradouro+cidade, depois o CEP, depois o centro da cidade;
   a precisão usada é informada na resposta (`STREET`, `POSTAL_CODE`, `CITY`).
4. **Overpass API (OpenStreetMap)** — farmácias (`amenity=pharmacy` ou `healthcare=pharmacy`) no raio (0,5–10 km; padrão 3 km).
5. Remoção de duplicados, distância aproximada em linha reta (Haversine), ordenação por proximidade e link do Google Maps.
   Nome, endereço, bairro, cidade/UF e telefone são exibidos **somente quando cadastrados no OpenStreetMap** — nada é inventado.

Erros tratados: CEP inválido (400), inexistente (404), sem coordenadas (422), provedores indisponíveis/timeout (503).
Cache, coalescência de requisições idênticas, limite de 1 req/s ao Nominatim, failover entre instâncias Overpass e
limite de 20 buscas/minuto por usuário. Para trocar de provedor, implemente `IGeocodingService` / `INearbyPlacesProvider`.

---

## 6. Telas do frontend × endpoints do backend

| Tela / rota | Endpoints |
|---|---|
| Login, cadastro (`/login`, `/register`) | Auth0 (Universal Login) |
| Início de sessão (todas as rotas protegidas) | `POST /api/auth/session`, `POST /api/auth/logout` |
| Onboarding / consentimento (`/onboarding`, `/consentimento`) | `POST /api/users/me/onboarding`, `POST /api/users/me/consents` |
| Visão geral (`/dashboard`) | `GET /api/dashboard` + ações rápidas da agenda |
| Pacientes (`/pacientes`) | `GET/POST /api/patients` |
| Paciente › Resumo | `GET/PUT/DELETE /api/patients/{id}`, `PATCH …/status`, `PUT …/measurement-units` |
| Paciente › Medições | `…/measurements`, `GET/PUT …/health-thresholds` |
| Paciente › Medicamentos | `…/medications/current`, CRUD `…/medications`, `…/discontinue`, `POST …/medication-administrations` |
| Paciente › Rotina | CRUD `…/care-routines` |
| Paciente › Compromissos | CRUD `…/events`, `PATCH …/events/{id}/status` |
| Paciente › Saúde | CRUD `…/health-conditions`, `…/allergies`, `…/surgeries` |
| Paciente › Diário | CRUD `…/care-diary`, `…/symptoms` |
| Paciente › Documentos | `GET/POST …/documents`, `GET …/documents/{id}/content`, `DELETE …/documents/{id}` |
| Paciente › Rede de apoio | `…/members`, `…/invitations` |
| Paciente › Histórico | `GET …/timeline`, `GET …/history` |
| Agenda (`/agenda`) | `GET /api/agenda`, `POST …/medication-administrations`, `POST/DELETE …/care-routines/{id}/completions`, `PATCH …/events/{id}/status` |
| Serviços próximos (`/servicos-proximos`) | `GET /api/locations/pharmacies` |
| Convites (`/convites`) | `GET /api/invitations/mine`, `POST /api/invitations/{id}/accept|decline` |
| Notificações (`/notificacoes`) | `GET /api/notifications`, `POST …/read`, `POST …/read-all`, `GET/PUT …/preferences` |
| Perfil (`/perfil`) | `GET/PUT/DELETE /api/users/me`, `GET /api/users/me/consents` |

(`…` = `/api/patients/{patientId}`). Erros seguem RFC 7807 com `code` estável; enums trafegam em `UPPER_SNAKE_CASE`.

---

## 7. Segurança (resumo)

- Tokens do Auth0 validados no backend (emissor, audiência, assinatura, expiração); PKCE no frontend; nenhum segredo no frontend.
- Autorização por paciente em toda operação (consultar/adicionar/alterar/excluir/gerenciar), com revogação imediata.
- LGPD: consentimentos versionados, CPF mascarado, exclusão definitiva de conta e de paciente.
- AES-256-GCM para dados sensíveis e anexos; trilha de auditoria com IP; timeout de sessão de 15 min; rate limiting;
  CORS restrito; cabeçalhos de segurança; validação de arquivos por assinatura binária (PDF/JPG/PNG até 10 MB).
