namespace Lacos.Domain.Enums;

/// <summary>
/// Situação do acompanhamento do paciente, usada no filtro "Todos os status" da tela de pacientes (WF02).
/// SUPOSIÇÃO: o wireframe exibe "Em acompanhamento"/"Em andamento" sem definir a lista completa de valores.
/// </summary>
public enum PatientCareStatus
{
    /// <summary>Em acompanhamento ativo.</summary>
    InFollowUp,
    /// <summary>Acompanhamento pausado temporariamente (ex.: internação).</summary>
    Paused,
    /// <summary>Acompanhamento encerrado; dados mantidos apenas para consulta.</summary>
    Discharged
}
