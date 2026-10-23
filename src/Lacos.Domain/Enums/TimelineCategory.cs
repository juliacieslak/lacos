namespace Lacos.Domain.Enums;

/// <summary>Categorias da linha do tempo do paciente (US04: filtrar por Medicamentos, Sintomas, Exames...).</summary>
public enum TimelineCategory
{
    Medication,
    MedicationAdministration,
    Measurement,
    Symptom,
    CareDiary,
    Event,
    Document,
    HealthCondition,
    Surgery,
    Allergy
}
