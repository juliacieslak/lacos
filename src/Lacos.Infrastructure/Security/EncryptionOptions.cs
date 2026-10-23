namespace Lacos.Infrastructure.Security;

/// <summary>
/// Chaves de criptografia (RNF02). NUNCA versionar chaves reais: em produção use variáveis de ambiente
/// ou um cofre de segredos (ex.: Supabase Vault, Azure Key Vault, AWS Secrets Manager).
/// Gere chaves com: <c>openssl rand -base64 32</c>.
/// </summary>
public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>Chave AES-256 em Base64 (32 bytes).</summary>
    public string DataKey { get; set; } = string.Empty;

    /// <summary>Chave HMAC-SHA256 em Base64 para blind index (32 bytes ou mais), distinta da chave de dados.</summary>
    public string BlindIndexKey { get; set; } = string.Empty;
}
