using Lacos.Application.DTOs;
using Lacos.Domain.Entities;
using Lacos.Domain.Interfaces;
using Lacos.Domain.Exceptions;
using Lacos.Domain.ValueObjects;
using Lacos.Application.Mappings;

namespace Lacos.Application.Services.Clinical;

/// <summary>Sintomas, queixas e alterações percebidas (RF09 / US02).</summary>
public sealed class SymptomService : PatientRecordService<SymptomRecord, SymptomRequest, SymptomResponse>
{
    public SymptomService(IPatientRecordRepository<SymptomRecord> repository, PatientRecordDependencies deps) : base(repository, deps) { }

    protected override string ResourceName => "Sintoma";

    // Regra: a data/hora da ocorrência é opcional (padrão: agora) e não pode estar no futuro.
    protected override Task<SymptomRecord> CreateEntityAsync(PatientAccess access, SymptomRequest r, CancellationToken ct)
    {
        var occurredAt = NowIfNull(r.OccurredAt);
        EnsureNotInFuture(occurredAt, nameof(r.OccurredAt));
        return Task.FromResult(new SymptomRecord(access.Patient.Id, access.UserId, r.Type, r.Description, occurredAt));
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, SymptomRecord e, SymptomRequest r, CancellationToken ct)
    {
        var occurredAt = r.OccurredAt ?? e.OccurredAt;
        EnsureNotInFuture(occurredAt, nameof(r.OccurredAt));
        e.Update(r.Type, r.Description, occurredAt, access.UserId);
        return Task.CompletedTask;
    }

    protected override SymptomResponse Map(SymptomRecord e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.Type, e.Description, e.OccurredAt, e.ToRecordInfo(names));
}

/// <summary>Diário de acompanhamento do paciente (RF04 / UC07).</summary>
public sealed class CareDiaryService : PatientRecordService<CareDiaryEntry, CareDiaryRequest, CareDiaryResponse>
{
    public CareDiaryService(IPatientRecordRepository<CareDiaryEntry> repository, PatientRecordDependencies deps) : base(repository, deps) { }

    protected override string ResourceName => "Diário de cuidado";

    protected override Task<CareDiaryEntry> CreateEntityAsync(PatientAccess access, CareDiaryRequest r, CancellationToken ct)
    {
        var occurredAt = NowIfNull(r.OccurredAt);
        EnsureNotInFuture(occurredAt, nameof(r.OccurredAt));
        return Task.FromResult(new CareDiaryEntry(access.Patient.Id, access.UserId, r.Category, r.Description, occurredAt));
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, CareDiaryEntry e, CareDiaryRequest r, CancellationToken ct)
    {
        var occurredAt = r.OccurredAt ?? e.OccurredAt;
        EnsureNotInFuture(occurredAt, nameof(r.OccurredAt));
        e.Update(r.Category, r.Description, occurredAt, access.UserId);
        return Task.CompletedTask;
    }

    protected override CareDiaryResponse Map(CareDiaryEntry e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.Category, e.Description, e.OccurredAt, e.ToRecordInfo(names));
}

/// <summary>Condições de saúde e transtornos com código CID (RF14).</summary>
public sealed class HealthConditionService : PatientRecordService<HealthCondition, HealthConditionRequest, HealthConditionResponse>
{
    public HealthConditionService(IPatientRecordRepository<HealthCondition> repository, PatientRecordDependencies deps) : base(repository, deps) { }

    protected override string ResourceName => "Condição de saúde";

    // Regras: CID em formato válido (CID-10/CID-11) e data de diagnóstico não futura e posterior ao nascimento.
    protected override Task<HealthCondition> CreateEntityAsync(PatientAccess access, HealthConditionRequest r, CancellationToken ct)
    {
        Validate(access, r);
        return Task.FromResult(new HealthCondition(access.Patient.Id, access.UserId, r.Name, r.CidCode, r.DiagnosisDate, r.Status, r.Notes));
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, HealthCondition e, HealthConditionRequest r, CancellationToken ct)
    {
        Validate(access, r);
        e.Update(r.Name, r.CidCode, r.DiagnosisDate, r.Status, r.Notes, access.UserId);
        return Task.CompletedTask;
    }

    protected override HealthConditionResponse Map(HealthCondition e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.Name, e.CidCode, e.DiagnosisDate, e.Status, e.Notes, e.ToRecordInfo(names));

    private void Validate(PatientAccess access, HealthConditionRequest r)
    {
        if (!CidCode.IsValid(r.CidCode))
            throw new ValidationFailedException(nameof(r.CidCode), "Código CID inválido (ex.: I10, E11.9 ou 5A11).");
        ClinicalDateRules.EnsureWithinLifetime(access.Patient, r.DiagnosisDate, Deps.Clock.LocalToday, nameof(r.DiagnosisDate));
    }
}

/// <summary>Histórico de cirurgias (RF15).</summary>
public sealed class SurgeryService : PatientRecordService<Surgery, SurgeryRequest, SurgeryResponse>
{
    public SurgeryService(IPatientRecordRepository<Surgery> repository, PatientRecordDependencies deps) : base(repository, deps) { }

    protected override string ResourceName => "Cirurgia";

    protected override Task<Surgery> CreateEntityAsync(PatientAccess access, SurgeryRequest r, CancellationToken ct)
    {
        ClinicalDateRules.EnsureWithinLifetime(access.Patient, r.PerformedOn, Deps.Clock.LocalToday, nameof(r.PerformedOn));
        return Task.FromResult(new Surgery(access.Patient.Id, access.UserId, r.ProcedureName, r.PerformedOn, r.Reason));
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, Surgery e, SurgeryRequest r, CancellationToken ct)
    {
        ClinicalDateRules.EnsureWithinLifetime(access.Patient, r.PerformedOn, Deps.Clock.LocalToday, nameof(r.PerformedOn));
        e.Update(r.ProcedureName, r.PerformedOn, r.Reason, access.UserId);
        return Task.CompletedTask;
    }

    protected override SurgeryResponse Map(Surgery e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.ProcedureName, e.PerformedOn, e.Reason, e.ToRecordInfo(names));
}

/// <summary>Alergias (RF16).</summary>
public sealed class AllergyService : PatientRecordService<Allergy, AllergyRequest, AllergyResponse>
{
    public AllergyService(IPatientRecordRepository<Allergy> repository, PatientRecordDependencies deps) : base(repository, deps) { }

    protected override string ResourceName => "Alergia";

    protected override Task<Allergy> CreateEntityAsync(PatientAccess access, AllergyRequest r, CancellationToken ct)
    {
        ClinicalDateRules.EnsureWithinLifetime(access.Patient, r.IdentifiedOn, Deps.Clock.LocalToday, nameof(r.IdentifiedOn));
        return Task.FromResult(new Allergy(access.Patient.Id, access.UserId, r.CausativeAgent, r.Manifestation, r.IdentifiedOn, r.Severity));
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, Allergy e, AllergyRequest r, CancellationToken ct)
    {
        ClinicalDateRules.EnsureWithinLifetime(access.Patient, r.IdentifiedOn, Deps.Clock.LocalToday, nameof(r.IdentifiedOn));
        e.Update(r.CausativeAgent, r.Manifestation, r.IdentifiedOn, r.Severity, access.UserId);
        return Task.CompletedTask;
    }

    protected override AllergyResponse Map(Allergy e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.CausativeAgent, e.Manifestation, e.IdentifiedOn, e.Severity, e.ToRecordInfo(names));
}

/// <summary>Regras de datas clínicas compartilhadas.</summary>
internal static class ClinicalDateRules
{
    /// <summary>
    /// Datas de histórico clínico (diagnóstico, cirurgia, identificação de alergia) devem estar entre
    /// o nascimento do paciente e hoje.
    /// </summary>
    public static void EnsureWithinLifetime(Patient patient, DateOnly? date, DateOnly today, string field)
    {
        if (date is null) return;
        if (date > today) throw new ValidationFailedException(field, "A data não pode estar no futuro.");
        if (date < patient.BirthDate) throw new ValidationFailedException(field, "A data não pode ser anterior ao nascimento do paciente.");
    }
}
