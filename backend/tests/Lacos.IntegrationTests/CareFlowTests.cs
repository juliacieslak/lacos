using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Lacos.IntegrationTests.Infrastructure;

namespace Lacos.IntegrationTests;

/// <summary>
/// Testes ponta a ponta dos fluxos principais descritos na documentação (Sprints 2 e 3).
/// </summary>
public sealed class CareFlowTests : ApiTestBase
{
    [Fact]
    public async Task Unauthenticated_requests_are_rejected()
    {
        var response = await Factory.CreateClient().GetAsync("/api/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Error_responses_keep_cors_headers_so_the_browser_can_read_them()
    {
        // sem o cabeçalho CORS o navegador bloqueia a resposta e o frontend só vê "falha de conexão".
        var client = Factory.CreateClientFor("auth0|cors", "cors@lacos.test");
        client.DefaultRequestHeaders.Add("Origin", "https://app.lacos.app");

        var error = await client.GetAsync("/api/patients"); // usuário ainda não registrado → 403
        error.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        error.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
        origins!.Should().Contain("https://app.lacos.app");
        (await ErrorCode(error)).Should().Be("USER_NOT_REGISTERED");
    }

    [Fact]
    public async Task Session_and_onboarding_flow_controls_access()
    {
        var client = Factory.CreateClientFor("auth0|ana", "ana@lacos.test", name: "Ana");

        // Primeiro acesso: usuário registrado com código "U" e sem convite (fluxo da Sprint 3).
        var session = await Read(await client.PostAsync("/api/auth/session", null));
        session.GetProperty("user").GetProperty("publicCode").GetString().Should().StartWith("U");
        session.GetProperty("accessStatus").GetString().Should().Be("NO_INVITATION");
        session.GetProperty("user").GetProperty("profileCompleted").GetBoolean().Should().BeFalse();

        // Sem onboarding, dados de pacientes ficam bloqueados.
        var blocked = await client.GetAsync("/api/patients");
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(blocked)).Should().Be("ONBOARDING_REQUIRED");

        // CPF inválido e falta de consentimento são rejeitados (RF01 / RNF01).
        var invalid = await client.PostAsJsonAsync("/api/users/me/onboarding", new
        {
            profileType = "FAMILY", name = "Ana", cpf = "111.111.111-11", birthDate = "1985-04-12", phone = "41999990000",
            profession = "Professora", cep = "80010-000", ethnicity = "PARDA", gender = "FEMALE",
            acceptTermsOfUse = false, acceptSensitiveDataProcessing = true
        }, Json);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await Read(invalid)).GetProperty("errors");
        errors.TryGetProperty("cpf", out _).Should().BeTrue();
        errors.TryGetProperty("acceptTermsOfUse", out _).Should().BeTrue();

        var ok = await client.PostAsJsonAsync("/api/users/me/onboarding", new
        {
            profileType = "FAMILY", name = "Ana", cpf = "526.018.159-06", birthDate = "1985-04-12", phone = "41999990000",
            profession = "Professora", cep = "80010-000", ethnicity = "PARDA", gender = "FEMALE",
            acceptTermsOfUse = true, acceptSensitiveDataProcessing = true
        }, Json);
        await EnsureStatus(ok, HttpStatusCode.OK);
        (await Read(ok)).GetProperty("cpfMasked").GetString().Should().Be("***.018.159-**");

        (await client.GetAsync("/api/patients")).StatusCode.Should().Be(HttpStatusCode.OK);

        // CPF duplicado entre usuários (blind index).
        var other = Factory.CreateClientFor("auth0|bia", "bia@lacos.test");
        await other.PostAsync("/api/auth/session", null);
        var duplicated = await other.PostAsJsonAsync("/api/users/me/onboarding", new
        {
            profileType = "CAREGIVER", name = "Bia", cpf = "52601815906", birthDate = "1990-01-01", phone = "41988887777",
            profession = "Cuidadora", cep = "80010000", ethnicity = "PRETA", gender = "FEMALE",
            acceptTermsOfUse = true, acceptSensitiveDataProcessing = true
        }, Json);
        duplicated.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCode(duplicated)).Should().Be("CPF_ALREADY_REGISTERED");
    }

    [Fact]
    public async Task Administrator_invites_caregiver_and_permissions_are_enforced()
    {
        var admin = await RegisteredUserAsync("auth0|admin", "admin@lacos.test", "08301661305", "Julia");
        var patient = await CreatePatientAsync(admin, "18609139034");
        var patientId = patient.GetProperty("id").GetString();
        patient.GetProperty("publicCode").GetString().Should().StartWith("P");
        patient.GetProperty("myRole").GetString().Should().Be("ADMINISTRATOR");
        patient.GetProperty("myPermissions").GetProperty("canManageMembers").GetBoolean().Should().BeTrue();

        // Convite com perfil "Apenas Leitura" (US01).
        var invite = await admin.PostAsJsonAsync($"/api/patients/{patientId}/invitations", new
        {
            email = "will@lacos.test", role = "CAREGIVER", relationshipType = "cuidador", preset = "READ_ONLY"
        }, Json);
        await EnsureStatus(invite, HttpStatusCode.Created);

        // O convidado vê o convite pendente ("aguardando aprovação") e aceita.
        var caregiver = Factory.CreateClientFor("auth0|will", "will@lacos.test", name: "Will");
        var session = await Read(await caregiver.PostAsync("/api/auth/session", null));
        session.GetProperty("accessStatus").GetString().Should().Be("PENDING_APPROVAL");
        var invitationId = session.GetProperty("pendingInvitations")[0].GetProperty("id").GetString();
        await EnsureStatus(await caregiver.PostAsJsonAsync("/api/users/me/onboarding", new
        {
            profileType = "CAREGIVER", name = "Will", cpf = "99603082430", birthDate = "1990-01-01", phone = "41988887777",
            profession = "Cuidador", cep = "80010000", ethnicity = "PRETA", gender = "MALE",
            acceptTermsOfUse = true, acceptSensitiveDataProcessing = true
        }, Json), HttpStatusCode.OK);
        var accepted = await caregiver.PostAsync($"/api/invitations/{invitationId}/accept", null);
        await EnsureStatus(accepted, HttpStatusCode.OK);
        var permissionId = (await Read(accepted)).GetProperty("permissionId").GetString();

        // Leitura permitida; escrita bloqueada (RF07).
        (await caregiver.GetAsync($"/api/patients/{patientId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        var symptom = new { type = "SYMPTOM", description = "Dor de cabeça leve" };
        var denied = await caregiver.PostAsJsonAsync($"/api/patients/{patientId}/symptoms", symptom, Json);
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // O administrador concede "Edição"; o cuidador registra e o administrador é notificado (US02 / RF12).
        await EnsureStatus(await admin.PutAsJsonAsync($"/api/patients/{patientId}/members/{permissionId}", new
        {
            role = "CAREGIVER", relationshipType = "cuidador", preset = "EDITOR"
        }, Json), HttpStatusCode.OK);
        var created = await caregiver.PostAsJsonAsync($"/api/patients/{patientId}/symptoms", symptom, Json);
        await EnsureStatus(created, HttpStatusCode.Created);
        (await Read(created)).GetProperty("record").GetProperty("createdByName").GetString().Should().Be("Will");

        var notifications = await Read(await admin.GetAsync("/api/notifications?unreadOnly=true"));
        notifications.GetProperty("unreadCount").GetInt32().Should().BeGreaterThan(0);
        notifications.GetProperty("page").GetProperty("items")[0].GetProperty("category").GetString().Should().Be("PATIENT_DATA_ADDED");

        // Histórico: inclusão registrada com o responsável (RF06 / RF10).
        var history = await Read(await admin.GetAsync($"/api/patients/{patientId}/history?entityType=SYMPTOM_RECORD"));
        var entry = history.GetProperty("items")[0];
        entry.GetProperty("action").GetString().Should().Be("CREATE");
        entry.GetProperty("userName").GetString().Should().Be("Will");
        entry.GetProperty("newValues").GetProperty("Description").GetString().Should().Be("Dor de cabeça leve");

        // Não é possível remover o único administrador.
        var members = await Read(await admin.GetAsync($"/api/patients/{patientId}/members"));
        var adminPermission = members.EnumerateArray().First(m => m.GetProperty("role").GetString() == "ADMINISTRATOR").GetProperty("permissionId").GetString();
        var lastManager = await admin.DeleteAsync($"/api/patients/{patientId}/members/{adminPermission}");
        (await ErrorCode(lastManager)).Should().Be("LAST_MANAGER");

        // Revogação com efeito imediato (US01 / UC05).
        (await admin.DeleteAsync($"/api/patients/{patientId}/members/{permissionId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await caregiver.GetAsync($"/api/patients/{patientId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Measurements_out_of_threshold_generate_alerts()
    {
        var admin = await RegisteredUserAsync("auth0|m1", "m1@lacos.test", "62819482112");
        var patientId = (await CreatePatientAsync(admin, "99351819019")).GetProperty("id").GetString();

        await EnsureStatus(await admin.PutAsJsonAsync($"/api/patients/{patientId}/health-thresholds", new
        {
            systolicPressure = new { min = 90, minNotice = true, max = 140, maxNotice = true },
            bloodGlucose = new { min = 70, minNotice = true, max = 180, maxNotice = true }
        }, Json), HttpStatusCode.OK);

        var response = await admin.PostAsJsonAsync($"/api/patients/{patientId}/measurements", new
        {
            systolic = 150, diastolic = 95, bloodGlucose = 110, description = "Após o almoço"
        }, Json);
        await EnsureStatus(response, HttpStatusCode.Created);
        var alerts = (await Read(response)).GetProperty("alerts");
        alerts.GetArrayLength().Should().Be(1);
        alerts[0].GetProperty("metric").GetString().Should().Be("SYSTOLIC_PRESSURE");

        var notifications = await Read(await admin.GetAsync("/api/notifications"));
        notifications.GetProperty("page").GetProperty("items").EnumerateArray()
            .Should().Contain(n => n.GetProperty("category").GetString() == "HEALTH_THRESHOLD_ALERT");

        // Valores fisiologicamente impossíveis são rejeitados.
        var invalid = await admin.PostAsJsonAsync($"/api/patients/{patientId}/measurements", new { oxygenSaturation = 140 }, Json);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Medication_schedule_appears_in_agenda_and_dose_can_be_registered()
    {
        var admin = await RegisteredUserAsync("auth0|med", "med@lacos.test", "93786579741");
        var patientId = (await CreatePatientAsync(admin, "54323194897")).GetProperty("id").GetString();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

        var med = await admin.PostAsJsonAsync($"/api/patients/{patientId}/medications", new
        {
            name = "Dipirona", dosage = "500 mg", brand = "Genérico", usageReason = "Dor", administrationRoute = "ORAL",
            prescribedBy = "Dr. Vinicius", startDate = today.AddDays(-1).ToString("yyyy-MM-dd"), frequency = "2x ao dia",
            administrationTimes = new[] { "00:00:00", "23:59:00" }
        }, Json);
        await EnsureStatus(med, HttpStatusCode.Created);
        var medicationId = (await Read(med)).GetProperty("id").GetString();

        var agenda = await Read(await admin.GetAsync($"/api/agenda?patientId={patientId}&date={today:yyyy-MM-dd}&view=DAY"));
        var doses = agenda.GetProperty("items").EnumerateArray().Where(i => i.GetProperty("kind").GetString() == "MEDICATION").ToList();
        doses.Should().HaveCount(2);
        var midnight = doses.First().GetProperty("startsAt").GetDateTimeOffset();

        // Registro rápido da dose programada (validação com o cuidador - Sprint 3).
        await EnsureStatus(await admin.PostAsJsonAsync($"/api/patients/{patientId}/medication-administrations", new
        {
            medicationId, scheduledFor = midnight, status = "ADMINISTERED"
        }, Json), HttpStatusCode.Created);
        var duplicate = await admin.PostAsJsonAsync($"/api/patients/{patientId}/medication-administrations", new
        {
            medicationId, scheduledFor = midnight, status = "ADMINISTERED"
        }, Json);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        agenda = await Read(await admin.GetAsync($"/api/agenda?patientId={patientId}&date={today:yyyy-MM-dd}&view=DAY"));
        agenda.GetProperty("items").EnumerateArray().First(i => i.GetProperty("kind").GetString() == "MEDICATION")
            .GetProperty("done").GetBoolean().Should().BeTrue();

        // Linha do tempo filtrada por categoria (US04).
        var timeline = await Read(await admin.GetAsync($"/api/patients/{patientId}/timeline?categories=MEDICATION_ADMINISTRATION"));
        timeline.GetProperty("items").GetArrayLength().Should().Be(1);
        timeline.GetProperty("items")[0].GetProperty("recordedByName").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Documents_accept_only_safe_formats_and_are_stored_encrypted()
    {
        var admin = await RegisteredUserAsync("auth0|doc", "doc@lacos.test", "52998224725");
        var patientId = (await CreatePatientAsync(admin, "11144477735")).GetProperty("id").GetString();
        var pdf = "%PDF-1.4\nreceita medica\n%%EOF"u8.ToArray();

        var upload = await admin.PostAsync($"/api/patients/{patientId}/documents", Multipart(pdf, "receita.pdf", "application/pdf"));
        await EnsureStatus(upload, HttpStatusCode.Created);
        var documentId = (await Read(upload)).GetProperty("id").GetString();

        var content = await admin.GetAsync($"/api/patients/{patientId}/documents/{documentId}/content");
        await EnsureStatus(content, HttpStatusCode.OK);
        (await content.Content.ReadAsByteArrayAsync()).Should().Equal(pdf);

        var exe = await admin.PostAsync($"/api/patients/{patientId}/documents", Multipart(new byte[] { 0x4D, 0x5A, 0, 0 }, "exame.pdf", "application/pdf"));
        exe.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patient_erasure_removes_access_and_clinical_data()
    {
        var admin = await RegisteredUserAsync("auth0|erase", "erase@lacos.test", "12345678909");
        var patient = await CreatePatientAsync(admin, "39053344705");
        var patientId = patient.GetProperty("id").GetString();
        await EnsureStatus(await admin.PostAsJsonAsync($"/api/patients/{patientId}/allergies", new
        {
            causativeAgent = "Penicilina", manifestation = "Urticária", severity = "SEVERE"
        }, Json), HttpStatusCode.Created);

        var wrongCode = new HttpRequestMessage(HttpMethod.Delete, $"/api/patients/{patientId}")
        {
            Content = JsonContent.Create(new { confirmationCode = "P999999", reason = "Solicitação da família" })
        };
        (await admin.SendAsync(wrongCode)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var erase = new HttpRequestMessage(HttpMethod.Delete, $"/api/patients/{patientId}")
        {
            Content = JsonContent.Create(new { confirmationCode = patient.GetProperty("publicCode").GetString(), reason = "Solicitação da família" })
        };
        await EnsureStatus(await admin.SendAsync(erase), HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/patients/{patientId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Read(await admin.GetAsync("/api/patients"))).GetArrayLength().Should().Be(0);
    }

    private static MultipartFormDataContent Multipart(byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent("PRESCRIPTION"), "category" },
            { new StringContent("Receita do cardiologista"), "description" }
        };
    }
}
