namespace Lacos.Domain.Common;

/// <summary>
/// Indica que a propriedade contém dado sensível (dados pessoais ou de saúde) e deve ser
/// armazenada criptografada com AES-256 (RNF02).
/// A infraestrutura aplica automaticamente um conversor de criptografia às propriedades marcadas
/// e também criptografa seus valores ao gravá-los na trilha de auditoria.
/// Observação: colunas criptografadas não podem ser pesquisadas por igualdade; quando isso é
/// necessário (ex.: unicidade de CPF), utiliza-se um "blind index" (hash HMAC) em coluna separada.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveDataAttribute : Attribute
{
}
