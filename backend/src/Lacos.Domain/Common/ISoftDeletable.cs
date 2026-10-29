namespace Lacos.Domain.Common;

/// <summary>
/// Entidades clínicas não são removidas fisicamente em uma exclusão comum: elas recebem uma
/// marcação de exclusão para que o histórico (RF06) continue consultável e para que a
/// rastreabilidade de quem excluiu e quando (RF10) seja preservada.
/// A exclusão física só ocorre no fluxo de exclusão definitiva de dados (LGPD - RNF01).
/// </summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
    Guid? DeletedByUserId { get; }

    /// <summary>Marca o registro como excluído, informando o responsável.</summary>
    void MarkAsDeleted(Guid userId, DateTimeOffset when);
}
