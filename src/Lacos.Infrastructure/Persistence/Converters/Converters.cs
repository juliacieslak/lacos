using System.Text;
using Lacos.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lacos.Infrastructure.Persistence.Converters;

/// <summary>
/// Converte enums para texto em UPPER_SNAKE_CASE (ex.: HealthProfessional → HEALTH_PROFESSIONAL),
/// mantendo os valores exatamente como previstos no modelo de dados (FAMILY, CAREGIVER, HEALTH_PROFESSIONAL, SCHEDULED...).
/// Armazenar texto (e não números) torna o banco legível e resistente a reordenação dos enums.
/// </summary>
public sealed class UpperSnakeEnumConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> ToDb = Enum.GetValues<TEnum>().ToDictionary(v => v, v => ToUpperSnake(v.ToString()));
    private static readonly Dictionary<string, TEnum> FromDb = ToDb.ToDictionary(kv => kv.Value, kv => kv.Key);

    public UpperSnakeEnumConverter() : base(v => ToDb[v], v => FromDb[v]) { }

    public static string ToUpperSnake(string name)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) sb.Append('_');
            sb.Append(char.ToUpperInvariant(name[i]));
        }
        return sb.ToString();
    }
}

/// <summary>
/// Conversor de colunas criptografadas (propriedades marcadas com [SensitiveData]).
/// Criptografa ao gravar e descriptografa ao ler, de forma transparente para o domínio (RNF02).
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryption)
        : base(v => encryption.Encrypt(v), v => encryption.Decrypt(v)) { }
}
