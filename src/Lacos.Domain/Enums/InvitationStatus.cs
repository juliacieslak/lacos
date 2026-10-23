namespace Lacos.Domain.Enums;

/// <summary>
/// Situação de um convite para integrar a rede de cuidado de um paciente.
/// Suporta o fluxo de navegação "Verificação de acesso" (Sprint 3): aguardando aprovação / sem convite / acesso autorizado.
/// </summary>
public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Revoked,
    Expired
}
