using Lacos.Application.DTOs;
using Lacos.Domain.Common;
using Lacos.Domain.Entities;
using Lacos.Domain.ValueObjects;

namespace Lacos.Application.Mappings;

/// <summary>
/// Mapeamentos manuais Entidade → DTO.
/// Motivo da escolha (em vez de bibliotecas de mapeamento por reflexão): mapeamento explícito é verificado
/// pelo compilador, não expõe propriedades sensíveis por acidente (ex.: CPF completo) e é mais performático.
/// </summary>
public static class MappingExtensions
{
    /// <summary>Converte o vínculo em DTO de permissões.</summary>
    public static PermissionsDto ToDto(this PermissionSet p) =>
        new(p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, p.CanManageMembers);

    /// <summary>Converte DTO em conjunto de permissões de domínio.</summary>
    public static PermissionSet ToDomain(this PermissionsDto p) =>
        new(p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, p.CanManageMembers);

    /// <summary>Monta os metadados de rastreabilidade com os nomes dos responsáveis (US04 / RF10).</summary>
    public static RecordInfoDto ToRecordInfo(this PatientRecord r, IReadOnlyDictionary<Guid, string> names) =>
        new(r.CreatedByUserId, names.GetValueOrDefault(r.CreatedByUserId),
            r.UpdatedByUserId, r.UpdatedByUserId is { } u ? names.GetValueOrDefault(u) : null,
            r.CreatedAt, r.UpdatedAt);

    /// <summary>Exibe o CPF mascarado; o CPF completo nunca é devolvido pela API (minimização de dados - LGPD).</summary>
    public static string? MaskCpf(string? cpf) =>
        cpf is not null && Cpf.TryCreate(cpf, out var parsed) ? parsed!.Masked : null;

    public static UserResponse ToResponse(this User u) =>
        new(u.Id, u.PublicCode, u.Name, u.Email, u.Phone, u.ProfileType, u.ProfileCompleted, u.IsActive,
            MaskCpf(u.Cpf), u.BirthDate, u.Profession, u.Cep, u.Ethnicity, u.Gender, u.CreatedAt);

    public static PatientResponse ToResponse(this Patient p, PatientUserPermission link, DateOnly today) =>
        new(p.Id, p.PublicCode, p.FullName, p.BirthDate, p.AgeOn(today), p.BirthPlace, p.Cep, p.Ethnicity,
            MaskCpf(p.Cpf), p.Gender, p.Email, p.Phone, p.HeightCm, p.CareStatus,
            new MeasurementUnitsDto(p.TemperatureUnit, p.GlucoseUnit, p.WeightUnit),
            link.Role, link.RelationshipType, link.ToPermissionSet().ToDto(), p.CreatedAt, p.UpdatedAt);

    public static CareTeamMemberResponse ToMemberResponse(this PatientUserPermission p) =>
        new(p.Id, p.UserId, p.User?.PublicCode ?? string.Empty, p.User?.Name ?? string.Empty, p.User?.Email ?? string.Empty,
            p.User?.ProfileType, p.Role, p.RelationshipType, p.ToPermissionSet().ToDto(), p.IsActive, p.CreatedAt, p.UpdatedAt);

    public static InvitationResponse ToResponse(this PatientInvitation i, string? invitedByName) =>
        new(i.Id, i.PatientId, i.Patient?.FullName ?? string.Empty, i.InvitedEmail, i.Role, i.RelationshipType,
            i.Permissions.Normalize().ToDto(), i.Status, i.ExpiresAt, invitedByName, i.CreatedAt);

    public static NotificationResponse ToResponse(this Notification n) =>
        new(n.Id, n.PatientId, n.Category, n.Title, n.Message, n.RelatedEntityId, n.CreatedAt, n.ReadAt);

    public static MetricLimitDto ToDto(this MetricLimit l) => new(l.Min, l.MinNotice, l.Max, l.MaxNotice);

    public static MetricLimit ToDomain(this MetricLimitDto d) => new(d.Min, d.MinNotice, d.Max, d.MaxNotice);
}
