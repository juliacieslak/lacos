namespace Lacos.Application.Common;

/// <summary>Ações controladas por permissão no perfil do paciente (RF07): consultar, adicionar, alterar, excluir e gerenciar membros.</summary>
public enum PatientPermission
{
    View,
    Add,
    Edit,
    Delete,
    ManageMembers
}

/// <summary>Situação de acesso do usuário, usada no fluxo "Verificação de acesso" da Sprint 3.</summary>
public enum AccessStatus
{
    /// <summary>Usuário possui ao menos um paciente com acesso ativo → Dashboard.</summary>
    Authorized,
    /// <summary>Existe convite pendente de aceite → Tela de acesso pendente.</summary>
    PendingApproval,
    /// <summary>Sem pacientes e sem convites → Tela sem convite (pode cadastrar um paciente e tornar-se administrador).</summary>
    NoInvitation
}
