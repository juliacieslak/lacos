using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services.Clinical;

/// <summary>
/// Configuração dos limites mínimos e máximos das métricas de saúde e da opção de alerta (RF13).
/// </summary>
public sealed class HealthThresholdService
{
    private readonly IHealthThresholdRepository _thresholds;
    private readonly PatientRecordDependencies _deps;

    public HealthThresholdService(IHealthThresholdRepository thresholds, PatientRecordDependencies deps)
    {
        _thresholds = thresholds;
        _deps = deps;
    }

    /// <summary>Retorna a configuração do paciente (ou uma configuração vazia, se ainda não existir).</summary>
    public async Task<HealthThresholdResponse> GetAsync(Guid patientId, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var threshold = await _thresholds.GetByPatientAsync(patientId, ct);
        await _deps.Audit.RecordViewAsync(access.UserId, patientId, "HEALTH_THRESHOLD", threshold?.Id, ct);
        return Map(patientId, threshold);
    }

    /// <summary>
    /// Cria ou atualiza (upsert) a configuração única do paciente.
    /// Permissão: adicionar (quando ainda não existe) ou alterar (quando já existe).
    /// Métricas não enviadas (null) permanecem inalteradas.
    /// </summary>
    public async Task<HealthThresholdResponse> UpsertAsync(Guid patientId, HealthThresholdRequest request, CancellationToken ct = default)
    {
        var existing = await _thresholds.GetByPatientAsync(patientId, ct);
        var access = await _deps.Access.RequireAsync(patientId, existing is null ? PatientPermission.Add : PatientPermission.Edit, ct);

        var threshold = existing ?? new HealthThreshold(patientId, access.UserId);
        Apply(threshold, HealthMetric.SystolicPressure, request.SystolicPressure, access.UserId);
        Apply(threshold, HealthMetric.DiastolicPressure, request.DiastolicPressure, access.UserId);
        Apply(threshold, HealthMetric.HeartRate, request.HeartRate, access.UserId);
        Apply(threshold, HealthMetric.Temperature, request.Temperature, access.UserId);
        Apply(threshold, HealthMetric.RespiratoryRate, request.RespiratoryRate, access.UserId);
        Apply(threshold, HealthMetric.OxygenSaturation, request.OxygenSaturation, access.UserId);
        Apply(threshold, HealthMetric.BloodGlucose, request.BloodGlucose, access.UserId);

        if (existing is null) await _thresholds.AddAsync(threshold, ct);
        await _deps.UnitOfWork.SaveChangesAsync(ct);
        return Map(patientId, threshold);
    }

    private static void Apply(HealthThreshold t, HealthMetric metric, MetricLimitDto? dto, Guid userId)
    {
        if (dto is not null) t.Configure(metric, dto.ToDomain(), userId);
    }

    private static HealthThresholdResponse Map(Guid patientId, HealthThreshold? t)
    {
        MetricLimitDto L(HealthMetric m) => t?.GetLimit(m).ToDto() ?? new MetricLimitDto(null, false, null, false);
        return new HealthThresholdResponse(t?.Id, patientId,
            L(HealthMetric.SystolicPressure), L(HealthMetric.DiastolicPressure), L(HealthMetric.HeartRate),
            L(HealthMetric.Temperature), L(HealthMetric.RespiratoryRate), L(HealthMetric.OxygenSaturation),
            L(HealthMetric.BloodGlucose), t?.CreatedByUserId, t?.UpdatedByUserId, t?.UpdatedAt);
    }
}
