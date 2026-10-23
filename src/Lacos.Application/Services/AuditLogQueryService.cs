using System.Text.Json;
using System.Text.Json.Nodes;
using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Consulta do histórico de inclusões, alterações e exclusões do paciente (RF06 / US01 "Exibe o nome do usuário
/// responsável por cada alteração realizada").
/// </summary>
public interface IAuditLogQueryService
{
    Task<PagedResult<AuditLogResponse>> ListAsync(Guid patientId, string? entityType, Guid? entityId, AuditAction? action,
        DateTimeOffset? from, DateTimeOffset? to, bool includeViews, PageQuery page, CancellationToken ct = default);
}

public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IPatientAccessService _access;
    private readonly IAuditLogRepository _logs;
    private readonly IUserRepository _users;
    private readonly IEncryptionService _encryption;

    public AuditLogQueryService(IPatientAccessService access, IAuditLogRepository logs, IUserRepository users, IEncryptionService encryption)
    {
        _access = access;
        _logs = logs;
        _users = users;
        _encryption = encryption;
    }

    /// <summary>
    /// Regras:
    /// - Usuários com permissão de consulta veem o histórico de alterações (RF06: "permitindo que usuários autorizados consultem").
    /// - Os registros de VISUALIZAÇÃO (log de acesso - RNF04) só podem ser consultados por gestores, pois revelam
    ///   o comportamento de outros membros.
    /// - Valores sensíveis, gravados criptografados na auditoria, são descriptografados apenas na resposta.
    /// </summary>
    public async Task<PagedResult<AuditLogResponse>> ListAsync(Guid patientId, string? entityType, Guid? entityId, AuditAction? action,
        DateTimeOffset? from, DateTimeOffset? to, bool includeViews, PageQuery page, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, includeViews || action == AuditAction.View
            ? PatientPermission.ManageMembers : PatientPermission.View, ct);

        var (items, total) = await _logs.ListByPatientAsync(patientId, entityType?.ToUpperInvariant(), entityId, action, from, to,
            includeViews, page.Skip, page.SafePageSize, ct);
        var names = await _users.GetNamesAsync(items.Where(i => i.UserId.HasValue).Select(i => i.UserId!.Value), ct);

        var mapped = items.Select(l => new AuditLogResponse(l.Id, l.UserId, l.UserId is { } u ? names.GetValueOrDefault(u) : "Sistema",
            l.EntityType, l.EntityId, l.Action, Decrypt(l.OldValues), Decrypt(l.NewValues), l.Reason,
            access.Permission.CanManageMembers ? l.IpAddress : null, l.CreatedAt)).ToList();
        return new PagedResult<AuditLogResponse>(mapped, page.SafePage, page.SafePageSize, total);
    }

    /// <summary>Percorre o JSON e descriptografa os valores marcados como cifrados pelo interceptador de auditoria.</summary>
    private JsonElement? Decrypt(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var node = JsonNode.Parse(json);
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                if (obj[key] is JsonValue v && v.TryGetValue<string>(out var s) && _encryption.IsEncrypted(s))
                {
                    try { obj[key] = _encryption.Decrypt(s); }
                    catch { obj[key] = "[indisponível]"; }
                }
            }
        }
        return JsonSerializer.Deserialize<JsonElement>(node!.ToJsonString());
    }
}
