using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services.Clinical;

/// <summary>Cadastro de medicamentos do paciente (RF08 / US02).</summary>
public sealed class MedicationService : PatientRecordService<Medication, MedicationRequest, MedicationResponse>
{
    private readonly IMedicationRepository _medications;

    public MedicationService(IMedicationRepository repository, PatientRecordDependencies deps) : base(repository, deps)
        => _medications = repository;

    protected override string ResourceName => "Medicamento";

    protected override Task<Medication> CreateEntityAsync(PatientAccess access, MedicationRequest r, CancellationToken ct) =>
        Task.FromResult(new Medication(access.Patient.Id, access.UserId, ToData(r)));

    protected override Task ApplyUpdateAsync(PatientAccess access, Medication e, MedicationRequest r, CancellationToken ct)
    {
        e.Update(ToData(r), access.UserId);
        return Task.CompletedTask;
    }

    protected override MedicationResponse Map(Medication e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.Name, e.Dosage, e.Brand, e.UsageReason, e.AdministrationRoute, e.PrescribedBy,
            e.StartDate, e.EndDate, e.Frequency, e.AdministrationTimes, e.DaysOfWeek,
            e.IsActiveOn(Deps.Clock.LocalToday), e.ToRecordInfo(names));

    /// <summary>
    /// Lista medicamentos do paciente; quando <paramref name="activeOnly"/> é verdadeiro, apenas os em uso hoje.
    /// </summary>
    public async Task<IReadOnlyList<MedicationResponse>> ListCurrentAsync(Guid patientId, bool activeOnly, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var meds = await _medications.ListByPatientsAsync(new[] { patientId }, activeOnly, Deps.Clock.LocalToday, ct);
        await Deps.Audit.RecordViewAsync(access.UserId, patientId, "MEDICATION_LIST", null, ct);
        var names = await LoadNamesAsync(meds, ct);
        return meds.Select(m => Map(m, names)).ToList();
    }

    /// <summary>Encerra o uso do medicamento (mantém o histórico, diferente de excluir).</summary>
    public async Task<MedicationResponse> DiscontinueAsync(Guid patientId, Guid id, DateOnly? endDate, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Edit, ct);
        var med = await FindAsync(patientId, id, ct);
        med.Discontinue(endDate ?? Deps.Clock.LocalToday, access.UserId);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
        return await MapAsync(med, ct);
    }

    private static MedicationData ToData(MedicationRequest r) =>
        new(r.Name, r.Dosage, r.Brand, r.UsageReason, r.AdministrationRoute, r.PrescribedBy, r.StartDate, r.EndDate,
            r.Frequency, r.AdministrationTimes ?? Array.Empty<TimeOnly>(), r.DaysOfWeek ?? Array.Empty<DayOfWeek>());
}

/// <summary>
/// Registro de doses administradas / não administradas (US02, RF12 e validação com cuidador da Sprint 3).
/// </summary>
public sealed class MedicationAdministrationService
    : PatientRecordService<MedicationAdministration, MedicationAdministrationRequest, MedicationAdministrationResponse>
{
    private readonly IMedicationRepository _medications;
    private readonly IMedicationAdministrationRepository _administrations;

    public MedicationAdministrationService(IMedicationAdministrationRepository repository, IMedicationRepository medications,
        PatientRecordDependencies deps) : base(repository, deps)
    {
        _administrations = repository;
        _medications = medications;
    }

    protected override string ResourceName => "Administração de medicamento";

    /// <summary>
    /// Regras:
    /// 1. O medicamento deve pertencer ao paciente.
    /// 2. Se a dose se refere a um horário programado (ScheduledFor), esse horário deve existir no esquema
    ///    do medicamento naquela data — garante a consistência da detecção de doses não administradas.
    /// 3. Não pode haver dois registros para a mesma dose programada.
    /// 4. Data/hora padrão = agora (US02) e não pode estar no futuro.
    /// </summary>
    protected override async Task<MedicationAdministration> CreateEntityAsync(PatientAccess access,
        MedicationAdministrationRequest r, CancellationToken ct)
    {
        var med = await _medications.GetForPatientAsync(access.Patient.Id, r.MedicationId, ct)
                  ?? throw new NotFoundException("Medicamento", r.MedicationId);

        if (r.ScheduledFor is { } scheduledFor)
        {
            var localDate = Deps.Clock.ToLocalDate(scheduledFor);
            var isScheduled = med.GetScheduledTimes(localDate)
                .Any(t => Deps.Clock.ToInstant(localDate, t) == scheduledFor);
            if (!isScheduled)
                throw new BusinessRuleException("SCHEDULE_NOT_FOUND", "O horário informado não corresponde a uma dose programada do medicamento.");
            if (await _administrations.ExistsForOccurrenceAsync(med.Id, scheduledFor, ct))
                throw new ConflictException("DOSE_ALREADY_RECORDED", "Esta dose programada já foi registrada.");
        }

        var administeredAt = NowIfNull(r.AdministeredAt);
        EnsureNotInFuture(administeredAt, nameof(r.AdministeredAt));
        return new MedicationAdministration(access.Patient.Id, med.Id, access.UserId, r.ScheduledFor, administeredAt, r.Status, r.Notes);
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, MedicationAdministration e, MedicationAdministrationRequest r, CancellationToken ct)
    {
        var administeredAt = r.AdministeredAt ?? e.AdministeredAt;
        EnsureNotInFuture(administeredAt, nameof(r.AdministeredAt));
        e.Update(administeredAt, r.Status, r.Notes, access.UserId);
        return Task.CompletedTask;
    }

    protected override MedicationAdministrationResponse Map(MedicationAdministration e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.MedicationId, e.Medication?.Name, e.ScheduledFor, e.AdministeredAt, e.Status, e.Notes, e.ToRecordInfo(names));

    // O nome do medicamento é carregado sob demanda quando a navegação não está preenchida.
    protected override async Task<MedicationAdministrationResponse> MapAsync(MedicationAdministration e, CancellationToken ct)
    {
        var response = await base.MapAsync(e, ct);
        if (response.MedicationName is not null) return response;
        var med = await _medications.GetForPatientAsync(e.PatientId, e.MedicationId, ct);
        return response with { MedicationName = med?.Name };
    }
}
