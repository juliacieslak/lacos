using System.Text.Json;
using Lacos.Application.Interfaces;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Registro explícito de eventos de auditoria que não são inclusões/alterações/exclusões de entidade
/// (essas são capturadas automaticamente pelo interceptador do EF Core):
/// - VIEW: visualização de dados médicos (RNF04).
/// - PROFILE_COMPLETED, ERASURE e demais ações de negócio.
/// </summary>
public interface IAuditService
{
    /// <summary>Registra a visualização de dados do paciente. É gravado imediatamente.</summary>
    Task RecordViewAsync(Guid userId, Guid patientId, string entityType, Guid? entityId, CancellationToken ct = default);

    /// <summary>Adiciona um registro de ação de negócio à unidade de trabalho corrente (gravado no próximo SaveChanges).</summary>
    Task RecordAsync(Guid? userId, Guid? patientId, string entityType, Guid? entityId, AuditAction action,
        object? newValues = null, string? reason = null, CancellationToken ct = default);
}

public sealed class AuditService : IAuditService
{
    private readonly IAuditLogRepository _logs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditService(IAuditLogRepository logs, IUnitOfWork unitOfWork, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _logs = logs;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task RecordViewAsync(Guid userId, Guid patientId, string entityType, Guid? entityId, CancellationToken ct = default)
    {
        await _logs.AddAsync(Build(userId, patientId, entityType, entityId, AuditAction.View, null, null), ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task RecordAsync(Guid? userId, Guid? patientId, string entityType, Guid? entityId, AuditAction action,
        object? newValues = null, string? reason = null, CancellationToken ct = default)
    {
        var json = newValues is null ? null : JsonSerializer.Serialize(newValues);
        return _logs.AddAsync(Build(userId, patientId, entityType, entityId, action, json, reason), ct);
    }

    private AuditLog Build(Guid? userId, Guid? patientId, string entityType, Guid? entityId, AuditAction action,
        string? newValues, string? reason) =>
        new(userId, patientId, entityType, entityId, action, null, newValues, reason,
            _currentUser.IpAddress, _currentUser.UserAgent, _clock.UtcNow);
}
