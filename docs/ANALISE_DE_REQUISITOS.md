# Análise de requisitos — Laços (back-end)

Este documento registra a análise feita **antes** da implementação, a partir de:

- *Documentação do Projeto – Sprint 2* (requisitos funcionais e não funcionais revisados, casos de uso, histórias de usuário, MoSCoW);
- *Documentação da Sprint 3* (wireframes WF01–WF03, fluxo de navegação, validações com stakeholders, matriz de rastreabilidade);
- *tabelasBancoDeDados_system_settings_onboarding.csv* (modelo de dados: `users`, `patient_user_permissions`, `health_thresholds`, `patient_events`, `notification_preferences`, `audit_logs`).

## 1. Atores

| Ator | Descrição (documentação) | Como o back-end o representa |
|---|---|---|
| Usuário Cadastrado | Qualquer ator autenticado (UC01, UC02, UC10) | `users` + token Auth0 |
| Administrador do Perfil | Familiar/responsável que cria o paciente e gerencia autorizações (UC03, UC04, UC05, UC09) | vínculo com `role = ADMINISTRATOR` e `can_manage_members = true` |
| Cuidador | Acompanhamento diário, rotina, medicação, sintomas (UC06, UC07, UC08, UC09) | vínculo com `role = CAREGIVER` e permissões definidas pelo administrador |
| Profissional da Saúde | Consulta histórico, exames, acompanha múltiplos pacientes (UC09, UC11) | vínculo com `role = HEALTH_PROFESSIONAL` |
| Sistema (automático) | Lembretes, alertas, auditoria | workers em segundo plano e interceptadores |

> Regra do modelo de dados: **o `profile_type` do usuário não concede acesso automático a nenhum paciente**. Todo acesso vem de `patient_user_permissions`.

## 2. Requisitos funcionais → implementação

| ID | Requisito (resumo) | Implementação |
|---|---|---|
| RF01 | Criar conta com nome, CPF, nascimento, e-mail, senha, telefone, profissão, CEP, etnia, gênero | Credenciais (e-mail/senha) no **Auth0**; `POST /api/auth/session` registra o usuário; `POST /api/users/me/onboarding` completa o cadastro com validação de CPF (dígitos verificadores), CEP, telefone e unicidade de CPF |
| RF02 | Login com e-mail e senha, acesso conforme perfil | Auth0 Universal Login + validação JWT (emissor, audiência, assinatura, expiração); permissões por paciente |
| RF03 | Administrador cria paciente (nome, nascimento, local, CEP, etnia, CPF, gênero, e-mail, telefone, altura, relação com o admin) | `POST /api/patients`; o criador vira Administrador do Perfil; `relationship_type` no vínculo |
| RF04 | Diário: atividades, alimentação, higiene, sono, medicação, cuidados, queixas e dores | `/api/patients/{id}/care-diary` (categorias do RF04) |
| RF05 | Consultar o perfil conforme permissões | `PatientAccessService` em toda operação; 404 sem vínculo, 403 sem a permissão |
| RF06 | Registrar inclusões/alterações/exclusões e permitir consultar o histórico | Interceptador de auditoria (mesma transação) + `GET /api/patients/{id}/history` |
| RF07 | Administrador define ações: consultar, adicionar, alterar, **excluir** | Flags granulares + presets "Apenas Leitura"/"Edição" (US01); coluna `can_delete_patient_data` acrescentada |
| RF08 | Medicamentos: nome, dosagem, marca, motivo, via, prescritor, início/fim, frequência e horários | `/api/patients/{id}/medications` (horários em `time[]`, dias da semana opcionais) |
| RF09 | Pressão, temperatura, FR, saturação, glicemia, dor, peso, sintomas, queixas, alterações — nas unidades do paciente | `/measurements` (com unidades do paciente e conversão) e `/symptoms` |
| RF10 | Identificar automaticamente o responsável e data/hora de cada ação | `created_by_user_id`/`updated_by_user_id`/`deleted_by_user_id` + `audit_logs.user_id` + IP |
| RF11 | Acompanhar mais de um paciente | `GET /api/patients`, agenda e dashboard multi-paciente |
| RF12 | Escolher categorias de notificação (novas informações, lembretes de eventos e medicações, medicação não administrada, métricas fora do limite) | `notification_preferences` + `health_threshold_alert` (acrescentada) + workers de lembretes |
| RF13 | Limites mínimo/máximo das métricas e opção de alerta | `health_thresholds` exatamente como no CSV; alerta quando o valor **atinge ou ultrapassa** o limite |
| RF14 | Condições de saúde com CID, data do diagnóstico, status (ativa, em remissão, inativa) | `/health-conditions` com validação de formato CID-10/CID-11 |
| RF15 | Cirurgias: procedimento, data e motivo | `/surgeries` |
| RF16 | Alergias: agente, manifestação, data de identificação, gravidade | `/allergies` |
| RF17 | Identificador único "U..." para usuários e "P..." para pacientes | `public_code` gerado por sequências PostgreSQL (`U000001`, `P000001`) |
| RF18 | Consultas, exames e atividades: tipo, descrição, data/hora, duração, local, profissional, resumo, anotações | `patient_events` exatamente como no CSV + transições de status |
| RF19 | Anexar imagens e documentos vinculados ao paciente | `/documents` (upload multipart, visualização inline, download) |

### Histórias de usuário (critérios de aceitação)

| História | Critério | Onde |
|---|---|---|
| US01 | Inserir e-mail do convidado; perfis "Apenas Leitura"/"Edição"; nome do responsável por alteração; revogação imediata; sem permissão não edita | Convites, presets, `history` com `userName`, permissões lidas do banco a cada requisição, 403 |
| US02 | Tipo de registro (medicamento, sintoma, métrica); data/hora/usuário automáticos; campos obrigatórios; < 2 s; notificar usuários configurados | Endpoints específicos, padrão "agora", FluentValidation, consultas indexadas/paginadas, `PATIENT_DATA_ADDED` |
| US03 | PDF/PNG/JPG; bloqueio > 10 MB; vínculo com paciente; pré-visualização; erro claro | Validação por extensão + content-type + assinatura binária; `content` inline; ProblemDetails |
| US04 | Ordem cronológica decrescente; filtro por categoria; quem registrou; abrir documentos | `GET /timeline?categories=...` |

### Wireframes e validações (Sprint 3)

- **WF01 Dashboard**: `GET /api/dashboard` — agenda de hoje (pendentes/concluídos), métricas recentes (pressão, glicemia...), "Quem está cuidando", notificações não lidas.
- **WF02 Pacientes**: `GET /api/patients?search=&status=` — idade, condições, status, "Cuidados de hoje (n)", "Próxima consulta".
- **WF03 Agenda**: `GET /api/agenda?view=DAY|WEEK|MONTH` — compromissos, doses e tarefas da rotina com indicação de feito/atrasado.
- **Fluxo de navegação**: `POST /api/auth/session` devolve `AUTHORIZED`, `PENDING_APPROVAL` ou `NO_INVITATION`.
- **Feedback do cuidador** (marcar cuidados e registrar sinais vitais rapidamente): `POST .../medication-administrations` (dose programada), `POST .../care-routines/{id}/completions`, `POST .../measurements`.

## 3. Requisitos não funcionais → implementação

| ID | Requisito | Implementação |
|---|---|---|
| RNF01 | LGPD: aceite dos termos, consentimento de dados sensíveis, exclusão definitiva | `user_consents` versionados com IP; filtro global bloqueia sem consentimento vigente; exclusão de conta (anonimização + Auth0) e de paciente (remoção física + redação da auditoria) |
| RNF02 | Criptografia AES-256 de dados sensíveis e anexos; HTTPS/TLS | AES-256-GCM via `[SensitiveData]` (CPF, diagnósticos, anotações clínicas...), anexos criptografados antes do storage, blind index HMAC para CPF, HSTS + redirecionamento HTTPS, SSL obrigatório na conexão Supabase |
| RNF03 | Timeout de 15 min de inatividade | `SessionActivityMiddleware` (expiração deslizante) + logout com revogação do token |
| RNF04 | Registro imutável com data, hora e IP ao visualizar, editar ou excluir | `audit_logs` com `ip_address`/`user_agent`, ação `VIEW`, trigger que bloqueia UPDATE/DELETE |
| RNF05/06 | Responsivo e WCAG | Responsabilidade do front-end; a API devolve dados prontos para exibição (agenda e dashboard agregados) |
| RNF07 | 10 MB por arquivo; PDF/JPG/PNG | `DocumentOptions.MaxFileSizeBytes`, limite do Kestrel, validação de assinatura |
| RNF08 | 99,9% de disponibilidade | Stateless, `/health`, retry de conexão (Npgsql), resiliência nas integrações (retry, circuit breaker, timeout) |
| RNF09 | < 2 s em operações rotineiras | Índices em `(patient_id, data)`, paginação, consultas em lote (sem N+1), `AsNoTracking` |
| RNF10 | Backup diário georredundante | Infraestrutura do Supabase (PITR / backups diários) — ver README |
| RNF11 | Notificações em até 1 minuto | Fila (`notifications`) + worker sinalizado imediatamente; verificação de lembretes a cada 60 s |

## 4. Entidades

Do modelo de dados (CSV): `users`, `patient_user_permissions`, `health_thresholds`, `patient_events`, `notification_preferences`, `audit_logs`.

Exigidas pelos requisitos e ausentes no CSV: `patients` (RF03), `user_consents` (RNF01), `patient_invitations` (US01 / fluxo de acesso), `medications` (RF08), `medication_administrations` (US02, RF12), `health_measurements` e `symptom_records` (RF09), `care_diary_entries` (RF04), `care_routine_tasks` e `care_routine_completions` (WF01/WF03, `care_routine_checkin`), `health_conditions` (RF14), `surgeries` (RF15), `allergies` (RF16), `patient_documents` (RF19), `notifications` (RF12/RNF11).

## 5. Suposições documentadas

Onde a documentação não define um detalhe, foi adotada a suposição abaixo (todas também comentadas no código):

1. **Autenticação**: o fluxo de navegação cita "Autenticação Auth0" e o CSV tem `auth0_user_id`; por isso e-mail/senha (RF01/RF02) ficam no Auth0 e o back-end **não armazena senhas**. O e-mail chega por claim (Auth0 Action) e precisa estar verificado para aceitar convites.
2. **Colunas acrescentadas ao CSV**: `users` (CPF, nascimento, profissão, CEP, etnia, gênero, `public_code`, `cpf_hash`, `anonymized_at`) por RF01/RF17; `patient_user_permissions.can_delete_patient_data` por RF07; `notification_preferences.health_threshold_alert` por RF12; `audit_logs.ip_address`/`user_agent` por RNF04.
3. **Formato do RF17**: letra + 6 dígitos sequenciais (ex.: `U000123`, `P000045`).
4. **Papéis** (`role`): ADMINISTRATOR, CAREGIVER, FAMILY_MEMBER, HEALTH_PROFESSIONAL (derivados dos atores). `relationship_type` é texto livre (ex.: filha, cuidador, médico).
5. **Convites**: por e-mail, validade de 7 dias (configurável). "Aguardando aprovação" = convite pendente de aceite.
6. **Paciente**: e-mail e telefone opcionais (pacientes idosos/dependentes podem não ter); CPF obrigatório e único; CPF imutável após o cadastro.
7. **Status do paciente** (filtro do WF02): IN_FOLLOW_UP, PAUSED, DISCHARGED. Lembretes automáticos só para pacientes IN_FOLLOW_UP.
8. **Etnia**: classificação do IBGE. **Gênero**: FEMALE, MALE, NON_BINARY, OTHER, PREFER_NOT_TO_SAY.
9. **Gravidade de alergia**: MILD, MODERATE, SEVERE, LIFE_THREATENING. **Via de administração**: vias farmacológicas usuais + OTHER.
10. **Frequência de medicamento**: texto descritivo + lista de horários + dias da semana opcionais; sem horários = "se necessário" (sem lembretes).
11. **Dose não administrada**: alerta quando passam 60 min (configurável) do horário programado sem registro; lembrete 15 min antes.
12. **Lembretes de compromissos**: 24 h e 1 h antes (configurável).
13. **Rotina de cuidado**: tarefas recorrentes diárias (ou em dias da semana) com marcação de concluída — derivada dos wireframes e de `care_routine_checkin`.
14. **Transições de status do evento**: SCHEDULED → COMPLETED/CANCELLED/MISSED; CANCELLED/MISSED → SCHEDULED; COMPLETED é final.
15. **Unidades canônicas dos limites**: mmHg, bpm, °C, rpm, %, mg/dL. As aferições podem usar °F, mmol/L e lb (unidades do paciente) e são convertidas para comparação.
16. **Exclusões comuns são lógicas** (preservam o histórico do RF06). A **exclusão definitiva** (LGPD) remove fisicamente os dados clínicos e arquivos, anonimiza o cadastro e redige os valores da auditoria, preservando apenas quem fez o quê e quando.
17. **Exclusão de conta**: bloqueada se o usuário for o único administrador de um paciente (evita pacientes sem gestor).
18. **Notificações padrão**: ao receber acesso a um paciente, todas as categorias ficam habilitadas (opt-out). As mensagens externas (e-mail/push) não contêm conteúdo clínico.
19. **Visualizações na auditoria** (log de acesso) só podem ser consultadas por administradores do paciente.
20. **Fuso horário de negócio**: `America/Sao_Paulo` (configurável).
21. **Insulina** (citada em RF13): o modelo de dados não tem limites de insulina; a glicemia cobre o controle glicêmico. Pode ser adicionada como nova métrica se necessário.
