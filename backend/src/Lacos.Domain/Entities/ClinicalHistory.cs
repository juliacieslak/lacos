using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.ValueObjects;

namespace Lacos.Domain.Entities;

/// <summary>
/// Condição de saúde ou transtorno do paciente (tabela <c>health_conditions</c>) — RF14.
/// Informa código CID, data do diagnóstico e status (ativa, em remissão ou inativa).
/// Diagnósticos são dados sensíveis e ficam criptografados (RNF02).
/// </summary>
public class HealthCondition : PatientRecord
{
    private HealthCondition() { }

    public HealthCondition(Guid patientId, Guid createdByUserId, string name, string cidCode, DateOnly? diagnosisDate,
        HealthConditionStatus status, string? notes) : base(patientId, createdByUserId)
    {
        Apply(name, cidCode, diagnosisDate, status, notes);
    }

    [SensitiveData] public string Name { get; private set; } = string.Empty;
    [SensitiveData] public string CidCode { get; private set; } = string.Empty;
    public DateOnly? DiagnosisDate { get; private set; }
    public HealthConditionStatus Status { get; private set; }
    [SensitiveData] public string? Notes { get; private set; }

    public override string AuditEntityType => "HEALTH_CONDITION";

    public void Update(string name, string cidCode, DateOnly? diagnosisDate, HealthConditionStatus status, string? notes, Guid userId)
    {
        Apply(name, cidCode, diagnosisDate, status, notes);
        TouchBy(userId);
    }

    private void Apply(string name, string cidCode, DateOnly? diagnosisDate, HealthConditionStatus status, string? notes)
    {
        Name = name.Trim();
        CidCode = ValueObjects.CidCode.Normalize(cidCode);
        DiagnosisDate = diagnosisDate;
        Status = status;
        Notes = notes?.Trim();
    }
}

/// <summary>
/// Cirurgia realizada pelo paciente (tabela <c>surgeries</c>) — RF15.
/// Informa nome do procedimento, data de realização e motivo.
/// </summary>
public class Surgery : PatientRecord
{
    private Surgery() { }

    public Surgery(Guid patientId, Guid createdByUserId, string procedureName, DateOnly performedOn, string reason)
        : base(patientId, createdByUserId)
    {
        ProcedureName = procedureName.Trim();
        PerformedOn = performedOn;
        Reason = reason.Trim();
    }

    public string ProcedureName { get; private set; } = string.Empty;
    public DateOnly PerformedOn { get; private set; }
    [SensitiveData] public string Reason { get; private set; } = string.Empty;

    public override string AuditEntityType => "SURGERY";

    public void Update(string procedureName, DateOnly performedOn, string reason, Guid userId)
    {
        ProcedureName = procedureName.Trim();
        PerformedOn = performedOn;
        Reason = reason.Trim();
        TouchBy(userId);
    }
}

/// <summary>
/// Alergia do paciente (tabela <c>allergies</c>) — RF16.
/// Informa agente causador, forma de manifestação, data de identificação e nível de gravidade.
/// </summary>
public class Allergy : PatientRecord
{
    private Allergy() { }

    public Allergy(Guid patientId, Guid createdByUserId, string causativeAgent, string manifestation,
        DateOnly? identifiedOn, AllergySeverity severity) : base(patientId, createdByUserId)
    {
        CausativeAgent = causativeAgent.Trim();
        Manifestation = manifestation.Trim();
        IdentifiedOn = identifiedOn;
        Severity = severity;
    }

    public string CausativeAgent { get; private set; } = string.Empty;
    [SensitiveData] public string Manifestation { get; private set; } = string.Empty;
    public DateOnly? IdentifiedOn { get; private set; }
    public AllergySeverity Severity { get; private set; }

    public override string AuditEntityType => "ALLERGY";

    public void Update(string causativeAgent, string manifestation, DateOnly? identifiedOn, AllergySeverity severity, Guid userId)
    {
        CausativeAgent = causativeAgent.Trim();
        Manifestation = manifestation.Trim();
        IdentifiedOn = identifiedOn;
        Severity = severity;
        TouchBy(userId);
    }
}
