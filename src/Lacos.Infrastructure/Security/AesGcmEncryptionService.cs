using System.Security.Cryptography;
using System.Text;
using Lacos.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Security;

/// <summary>
/// Criptografia autenticada AES-256-GCM (RNF02).
///
/// Motivo da escolha: o GCM, além de sigilo, garante integridade (tag de autenticação de 128 bits);
/// qualquer adulteração do valor no banco é detectada na leitura.
///
/// Formato de saída (texto): "enc:v1:" + Base64(nonce[12] | tag[16] | ciphertext).
/// - Nonce aleatório de 96 bits por operação (nunca reutilizado com a mesma chave).
/// - O prefixo versionado permite rotação de chave/algoritmo no futuro (v2...) sem quebrar dados existentes.
/// </summary>
public sealed class AesGcmEncryptionService : IEncryptionService
{
    public const string Prefix = "enc:v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] BinaryMagic = "LCv1"u8.ToArray();

    private readonly byte[] _key;

    public AesGcmEncryptionService(IOptions<EncryptionOptions> options)
    {
        _key = DecodeKey(options.Value.DataKey, "Encryption:DataKey", exactLength: 32);
    }

    public bool IsEncrypted(string? value) => value is not null && value.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Criptografa um texto. Valores já criptografados não são criptografados novamente.</summary>
    public string Encrypt(string plainText)
    {
        if (IsEncrypted(plainText)) return plainText;
        return Prefix + Convert.ToBase64String(EncryptCore(Encoding.UTF8.GetBytes(plainText)));
    }

    /// <summary>Descriptografa; valores sem prefixo (legados em texto claro) são devolvidos como estão.</summary>
    public string Decrypt(string cipherText)
    {
        if (!IsEncrypted(cipherText)) return cipherText;
        var payload = Convert.FromBase64String(cipherText[Prefix.Length..]);
        return Encoding.UTF8.GetString(DecryptCore(payload));
    }

    /// <summary>Criptografa arquivos: "LCv1" + nonce + tag + conteúdo cifrado.</summary>
    public byte[] EncryptBytes(byte[] plain) => BinaryMagic.Concat(EncryptCore(plain)).ToArray();

    public byte[] DecryptBytes(byte[] cipher)
    {
        if (cipher.Length < BinaryMagic.Length || !cipher.AsSpan(0, BinaryMagic.Length).SequenceEqual(BinaryMagic))
            throw new CryptographicException("Arquivo em formato de criptografia desconhecido.");
        return DecryptCore(cipher[BinaryMagic.Length..]);
    }

    private byte[] EncryptCore(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipher = new byte[plain.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, result, NonceSize + TagSize, cipher.Length);
        return result;
    }

    private byte[] DecryptCore(byte[] payload)
    {
        if (payload.Length < NonceSize + TagSize) throw new CryptographicException("Conteúdo criptografado inválido.");
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    internal static byte[] DecodeKey(string base64, string name, int exactLength = 0, int minLength = 0)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException($"A configuração '{name}' é obrigatória (chave Base64).");
        byte[] key;
        try { key = Convert.FromBase64String(base64); }
        catch (FormatException) { throw new InvalidOperationException($"A configuração '{name}' não é Base64 válido."); }
        if (exactLength > 0 && key.Length != exactLength)
            throw new InvalidOperationException($"A configuração '{name}' deve ter {exactLength} bytes (AES-256).");
        if (minLength > 0 && key.Length < minLength)
            throw new InvalidOperationException($"A configuração '{name}' deve ter ao menos {minLength} bytes.");
        return key;
    }
}
