namespace Lacos.Domain.Common;

/// <summary>
/// Classe base de todas as entidades persistidas.
/// Centraliza o identificador (uuid) e os carimbos de data/hora de criação e alteração,
/// presentes em todas as tabelas do modelo de dados fornecido (created_at / updated_at).
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Identificador único (chave primária uuid).</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Data e hora de criação do registro (UTC). Preenchido automaticamente pela infraestrutura.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Data e hora da última alteração (UTC). Atualizado automaticamente pela infraestrutura.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
