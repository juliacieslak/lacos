namespace Lacos.Application.Interfaces;

/// <summary>
/// Criptografia simétrica AES-256 (RNF02) para dados sensíveis e arquivos anexados.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    bool IsEncrypted(string? value);
    byte[] EncryptBytes(byte[] plain);
    byte[] DecryptBytes(byte[] cipher);
}

/// <summary>
/// "Blind index": hash HMAC-SHA256 determinístico de um valor sensível, permitindo verificar unicidade
/// (ex.: CPF) sem armazenar ou comparar o valor em texto claro.
/// </summary>
public interface IBlindIndexService
{
    string Compute(string normalizedValue);
}

/// <summary>Gera identificadores legíveis sequenciais: "U..." para usuários e "P..." para pacientes (RF17).</summary>
public interface IPublicCodeGenerator
{
    Task<string> NextUserCodeAsync(CancellationToken ct = default);
    Task<string> NextPatientCodeAsync(CancellationToken ct = default);
}
