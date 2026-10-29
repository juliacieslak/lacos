using Lacos.Application.Common;
using Lacos.Domain.Entities;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>Resultado de uma verificação de acesso: quem é o usuário, qual paciente e quais permissões possui.</summary>
public sealed record PatientAccess(Guid UserId, Patient Patient, PatientUserPermission Permission);

/// <summary>
/// Serviço central de autorização por paciente (RF05, RF07, RF11, US01).
/// Toda operação sobre dados de paciente passa por aqui antes de qualquer leitura ou escrita.
/// </summary>
public interface IPatientAccessService
{
    Task<PatientAccess> RequireAsync(Guid patientId, PatientPermission permission, CancellationToken ct = default);
}

public sealed class PatientAccessService : IPatientAccessService
{
    private readonly ICurrentUserContext _userContext;
    private readonly IPatientRepository _patients;
    private readonly IPatientPermissionRepository _permissions;

    public PatientAccessService(ICurrentUserContext userContext, IPatientRepository patients, IPatientPermissionRepository permissions)
    {
        _userContext = userContext;
        _patients = patients;
        _permissions = permissions;
    }

    /// <summary>
    /// Valida se o usuário atual pode executar a ação no paciente.
    /// Regras:
    /// 1. Sem vínculo ativo → 404 (não revela a existência do paciente a terceiros, evitando enumeração).
    /// 2. Paciente excluído definitivamente (LGPD) → 404.
    /// 3. Vínculo ativo sem a permissão exigida → 403 (o front-end não deve exibir telas de edição - US01).
    /// As permissões são lidas do banco a cada chamada; por isso a revogação tem efeito imediato (US01).
    /// </summary>
    public async Task<PatientAccess> RequireAsync(Guid patientId, PatientPermission permission, CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        var link = await _permissions.GetAsync(patientId, userId, ct);
        if (link is null || !link.IsActive) throw new NotFoundException("Paciente", patientId);

        var patient = await _patients.GetByIdAsync(patientId, ct);
        if (patient is null || patient.ErasedAt is not null) throw new NotFoundException("Paciente", patientId);

        var allowed = permission switch
        {
            PatientPermission.View => link.CanViewPatient,
            PatientPermission.Add => link.CanAddPatientData,
            PatientPermission.Edit => link.CanEditPatientData,
            PatientPermission.Delete => link.CanDeletePatientData,
            PatientPermission.ManageMembers => link.CanManageMembers,
            _ => false
        };
        if (!allowed)
            throw new ForbiddenException($"Você não possui permissão para {Describe(permission)} neste paciente.");

        return new PatientAccess(userId, patient, link);
    }

    private static string Describe(PatientPermission p) => p switch
    {
        PatientPermission.View => "consultar informações",
        PatientPermission.Add => "adicionar informações",
        PatientPermission.Edit => "alterar informações",
        PatientPermission.Delete => "excluir informações",
        PatientPermission.ManageMembers => "gerenciar a rede de cuidado",
        _ => "executar esta ação"
    };
}
