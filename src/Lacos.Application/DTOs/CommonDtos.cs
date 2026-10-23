using Lacos.Domain.Enums;

namespace Lacos.Application.DTOs;

/// <summary>Permissões granulares do usuário em um paciente (RF07).</summary>
public sealed record PermissionsDto(bool CanView, bool CanAdd, bool CanEdit, bool CanDelete, bool CanManageMembers);

/// <summary>
/// Metadados de rastreabilidade incluídos em todo registro clínico.
/// Atende US04 ("Exibe quem realizou cada um dos registros listados") e RF10.
/// </summary>
public sealed record RecordInfoDto(
    Guid CreatedByUserId, string? CreatedByName,
    Guid? UpdatedByUserId, string? UpdatedByName,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>Unidades de medida do paciente (RF09).</summary>
public sealed record MeasurementUnitsDto(TemperatureUnit Temperature, GlucoseUnit Glucose, WeightUnit Weight);
