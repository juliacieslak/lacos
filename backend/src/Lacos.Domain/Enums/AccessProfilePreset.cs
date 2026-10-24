namespace Lacos.Domain.Enums;

/// <summary>
/// Perfis de acesso pré-definidos exibidos ao convidar/configurar um membro (US01:
/// "Exibe opções de perfis de acesso (ex: 'Apenas Leitura' e 'Edição')").
/// Os presets apenas preenchem as permissões granulares (RF07); <see cref="Custom"/> permite configurar cada flag.
/// </summary>
public enum AccessProfilePreset
{
    /// <summary>Apenas leitura: somente consultar.</summary>
    ReadOnly,
    /// <summary>Edição: consultar, adicionar, alterar e excluir informações.</summary>
    Editor,
    /// <summary>Administração: edição + gerenciar membros da rede de cuidado.</summary>
    Manager,
    /// <summary>Permissões informadas individualmente.</summary>
    Custom
}
