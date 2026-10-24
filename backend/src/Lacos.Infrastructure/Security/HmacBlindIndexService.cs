using System.Security.Cryptography;
using System.Text;
using Lacos.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Security;

/// <summary>
/// Blind index com HMAC-SHA256: gera um hash determinístico e com chave secreta para valores sensíveis.
/// Permite índices únicos (ex.: CPF) sem armazenar o valor em claro; sem a chave, o hash não pode ser
/// revertido por força bruta (diferente de um SHA-256 simples sobre os ~10^9 CPFs possíveis).
/// </summary>
public sealed class HmacBlindIndexService : IBlindIndexService
{
    private readonly byte[] _key;

    public HmacBlindIndexService(IOptions<EncryptionOptions> options)
    {
        _key = AesGcmEncryptionService.DecodeKey(options.Value.BlindIndexKey, "Encryption:BlindIndexKey", minLength: 32);
    }

    public string Compute(string normalizedValue) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(normalizedValue))).ToLowerInvariant();
}
