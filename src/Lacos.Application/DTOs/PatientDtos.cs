using Lacos.Domain.Enums;

namespace Lacos.Application.DTOs;

/// <summary>Cadastro de paciente (RF03). <see cref="RelationshipType"/> = relação do administrador com o paciente.</summary>
public sealed record CreatePatientRequest(
    string FullName, DateOnly BirthDate, string BirthPlace, string Cep, Ethnicity Ethnicity, string Cpf,
    Gender Gender, string? Email, string? Phone, int HeightCm, string RelationshipType);

public sealed record UpdatePatientRequest(
    string FullName, DateOnly BirthDate, string BirthPlace, string Cep, Ethnicity Ethnicity,
    Gender Gender, string? Email, string? Phone, int HeightCm);

public sealed record ChangeCareStatusRequest(PatientCareStatus Status, string? Reason);

public sealed record UpdateMeasurementUnitsRequest(TemperatureUnit Temperature, GlucoseUnit Glucose, WeightUnit Weight);

/// <summary>Exclusão definitiva do paciente (LGPD). Exige informar o código público do paciente como confirmação.</summary>
public sealed record ErasePatientRequest(string ConfirmationCode, string Reason);

public sealed record PatientResponse(
    Guid Id, string PublicCode, string FullName, DateOnly BirthDate, int Age, string BirthPlace, string Cep,
    Ethnicity Ethnicity, string? CpfMasked, Gender Gender, string? Email, string? Phone, int HeightCm,
    PatientCareStatus CareStatus, MeasurementUnitsDto Units, CareRole MyRole, string MyRelationshipType,
    PermissionsDto MyPermissions, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>Item da lista "Meus pacientes" (WF02): idade, condições, status, cuidados de hoje e próxima consulta.</summary>
public sealed record PatientSummaryResponse(
    Guid Id, string PublicCode, string FullName, int Age, PatientCareStatus CareStatus,
    IReadOnlyList<string> ActiveConditions, int TodayCareItems, int TodayPendingCareItems,
    DateTimeOffset? NextAppointmentAt, string? NextAppointmentTitle, CareRole MyRole, PermissionsDto MyPermissions);
