using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;
using Lacos.Domain.ValueObjects;

namespace Lacos.Application.Services;

/// <summary>
/// Perfil do paciente: criação (UC03 / RF03), consulta (RF05), edição, lista de pacientes acompanhados (RF11 / UC11 / WF02)
/// e exclusão definitiva (LGPD).
/// </summary>
public interface IPatientService
{
    Task<PatientResponse> CreateAsync(CreatePatientRequest request, CancellationToken ct = default);
    Task<PatientResponse> GetAsync(Guid patientId, CancellationToken ct = default);
    Task<PatientResponse> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PatientSummaryResponse>> ListMineAsync(string? search, PatientCareStatus? status, CancellationToken ct = default);
    Task<PatientResponse> ChangeCareStatusAsync(Guid patientId, ChangeCareStatusRequest request, CancellationToken ct = default);
    Task<PatientResponse> UpdateMeasurementUnitsAsync(Guid patientId, UpdateMeasurementUnitsRequest request, CancellationToken ct = default);
    Task EraseAsync(Guid patientId, ErasePatientRequest request, CancellationToken ct = default);
}

public sealed class PatientService : IPatientService
{
    private readonly ICurrentUserContext _userContext;
    private readonly IPatientAccessService _access;
    private readonly IPatientRepository _patients;
    private readonly IPatientPermissionRepository _permissions;
    private readonly IPatientInvitationRepository _invitations;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly IHealthConditionRepository _conditions;
    private readonly IPatientEventRepository _events;
    private readonly IPatientDataEraser _eraser;
    private readonly IAgendaBuilder _agenda;
    private readonly IPublicCodeGenerator _codes;
    private readonly IBlindIndexService _blindIndex;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public PatientService(ICurrentUserContext userContext, IPatientAccessService access, IPatientRepository patients,
        IPatientPermissionRepository permissions, IPatientInvitationRepository invitations, INotificationPreferenceRepository preferences,
        IHealthConditionRepository conditions, IPatientEventRepository events, IPatientDataEraser eraser, IAgendaBuilder agenda,
        IPublicCodeGenerator codes, IBlindIndexService blindIndex, IAuditService audit, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _userContext = userContext;
        _access = access;
        _patients = patients;
        _permissions = permissions;
        _invitations = invitations;
        _preferences = preferences;
        _conditions = conditions;
        _events = events;
        _eraser = eraser;
        _agenda = agenda;
        _codes = codes;
        _blindIndex = blindIndex;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Cria o perfil do paciente (UC03). Regras:
    /// - Qualquer usuário com cadastro concluído pode criar um paciente e torna-se seu Administrador do Perfil
    ///   (todas as permissões, inclusive gerenciar membros) — o profile_type não interfere (CSV).
    /// - CPF válido e único entre pacientes (blind index).
    /// - O código público "P..." é gerado automaticamente (RF17).
    /// - O administrador recebe preferências de notificação padrão.
    /// </summary>
    public async Task<PatientResponse> CreateAsync(CreatePatientRequest r, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        if (!user.ProfileCompleted)
            throw new BusinessRuleException("ONBOARDING_REQUIRED", "Conclua seu cadastro antes de criar um paciente.");

        var cpf = Cpf.Create(r.Cpf);
        var cpfHash = _blindIndex.Compute(cpf.Value);
        if (await _patients.ExistsByCpfHashAsync(cpfHash, null, ct))
            throw new ConflictException("PATIENT_CPF_ALREADY_REGISTERED",
                "Já existe um paciente com este CPF. Solicite um convite ao administrador do perfil existente.");

        var patient = new Patient(await _codes.NextPatientCodeAsync(ct), user.Id, r.FullName, r.BirthDate, r.BirthPlace,
            Cep.Create(r.Cep).Value, r.Ethnicity, cpf.Value, cpfHash, r.Gender, r.Email, r.Phone, r.HeightCm);
        var adminLink = PatientUserPermission.CreateAdministrator(patient.Id, user.Id, r.RelationshipType);

        await _patients.AddAsync(patient, ct);
        await _permissions.AddAsync(adminLink, ct);
        await _preferences.AddAsync(new NotificationPreference(patient.Id, user.Id), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return patient.ToResponse(adminLink, _clock.LocalToday);
    }

    /// <summary>Consulta o perfil (RF05). Registra a visualização na auditoria (RNF04).</summary>
    public async Task<PatientResponse> GetAsync(Guid patientId, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.View, ct);
        await _audit.RecordViewAsync(access.UserId, patientId, "PATIENT", patientId, ct);
        return access.Patient.ToResponse(access.Permission, _clock.LocalToday);
    }

    /// <summary>Edita os dados cadastrais (permissão de alterar). O CPF não pode ser alterado.</summary>
    public async Task<PatientResponse> UpdateAsync(Guid patientId, UpdatePatientRequest r, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.Edit, ct);
        access.Patient.Update(r.FullName, r.BirthDate, r.BirthPlace, Cep.Create(r.Cep).Value, r.Ethnicity, r.Gender, r.Email, r.Phone, r.HeightCm);
        await _unitOfWork.SaveChangesAsync(ct);
        return access.Patient.ToResponse(access.Permission, _clock.LocalToday);
    }

    /// <summary>
    /// Lista os pacientes acompanhados pelo usuário (RF11 / UC11 / WF02), com busca por nome e filtro por status.
    /// Para cada paciente: idade, condições ativas, cuidados de hoje (total e pendentes) e próxima consulta.
    /// As consultas auxiliares são feitas em lote para todos os pacientes (evita N+1).
    /// </summary>
    public async Task<IReadOnlyList<PatientSummaryResponse>> ListMineAsync(string? search, PatientCareStatus? status, CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        var accessible = await _patients.ListAccessibleAsync(userId, search?.Trim(), status, ct);
        if (accessible.Count == 0) return Array.Empty<PatientSummaryResponse>();

        var ids = accessible.Select(a => a.Patient.Id).ToList();
        var today = _clock.LocalToday;
        var conditions = (await _conditions.ListActiveByPatientsAsync(ids, ct)).ToLookup(c => c.PatientId);
        var agenda = (await _agenda.BuildAsync(accessible.ToDictionary(a => a.Patient.Id, a => a.Patient.FullName), today, today, ct))
            .ToLookup(i => i.PatientId);
        var upcoming = (await _events.ListInRangeAsync(ids, _clock.UtcNow, _clock.UtcNow.AddYears(1), PatientEventType.Appointment,
                PatientEventStatus.Scheduled, ct))
            .GroupBy(e => e.PatientId).ToDictionary(g => g.Key, g => g.OrderBy(e => e.ScheduledAt).First());

        var result = new List<PatientSummaryResponse>();
        foreach (var (patient, link) in accessible)
        {
            upcoming.TryGetValue(patient.Id, out var next);
            var items = agenda[patient.Id].ToList();
            result.Add(new PatientSummaryResponse(patient.Id, patient.PublicCode, patient.FullName, patient.AgeOn(today), patient.CareStatus,
                conditions[patient.Id].Select(c => c.Name).ToList(), items.Count, items.Count(i => !i.Done),
                next?.ScheduledAt, next?.Title, link.Role, link.ToPermissionSet().ToDto()));
            await _audit.RecordViewAsync(userId, patient.Id, "PATIENT_SUMMARY", patient.Id, ct);
        }
        return result;
    }

    /// <summary>Altera a situação do acompanhamento (auditado como STATUS_CHANGE). Permissão: alterar.</summary>
    public async Task<PatientResponse> ChangeCareStatusAsync(Guid patientId, ChangeCareStatusRequest r, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.Edit, ct);
        access.Patient.ChangeCareStatus(r.Status);
        await _unitOfWork.SaveChangesAsync(ct);
        return access.Patient.ToResponse(access.Permission, _clock.LocalToday);
    }

    /// <summary>Define as unidades de medida do paciente (RF09). Permissão: alterar.</summary>
    public async Task<PatientResponse> UpdateMeasurementUnitsAsync(Guid patientId, UpdateMeasurementUnitsRequest r, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.Edit, ct);
        access.Patient.SetMeasurementUnits(r.Temperature, r.Glucose, r.Weight);
        await _unitOfWork.SaveChangesAsync(ct);
        return access.Patient.ToResponse(access.Permission, _clock.LocalToday);
    }

    /// <summary>
    /// Exclusão definitiva dos dados do paciente (RNF01 - LGPD). Somente gestores da rede de cuidado.
    /// Fluxo:
    /// 1. Confirmação: o código público do paciente deve ser digitado (evita exclusões acidentais).
    /// 2. Remove fisicamente todos os registros clínicos e arquivos e redige os valores da auditoria.
    /// 3. Revoga vínculos, convites e preferências.
    /// 4. Anonimiza o cadastro do paciente e registra a ação ERASURE com o motivo.
    /// </summary>
    public async Task EraseAsync(Guid patientId, ErasePatientRequest r, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.ManageMembers, ct);
        if (!string.Equals(r.ConfirmationCode?.Trim(), access.Patient.PublicCode, StringComparison.OrdinalIgnoreCase))
            throw new ValidationFailedException(nameof(r.ConfirmationCode), "Informe o código do paciente para confirmar a exclusão definitiva.");

        await _eraser.EraseClinicalDataAsync(patientId, ct);

        foreach (var link in await _permissions.ListByPatientAsync(patientId, includeInactive: false, ct)) link.Revoke();
        foreach (var invitation in await _invitations.ListByPatientAsync(patientId, ct))
            if (invitation.Status == InvitationStatus.Pending) invitation.Revoke(_clock.UtcNow);

        access.Patient.Erase(_clock.UtcNow);
        await _audit.RecordAsync(access.UserId, patientId, "PATIENT", patientId, AuditAction.Erasure, reason: r.Reason, ct: ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
