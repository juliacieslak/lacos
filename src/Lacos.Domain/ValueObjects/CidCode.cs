using System.Text.RegularExpressions;

namespace Lacos.Domain.ValueObjects;

/// <summary>
/// Código CID da condição de saúde (RF14).
/// Aceita os formatos CID-10 (ex.: I10, E11.9, F03) e CID-11 (ex.: 5A11, 8A20.0, BA00.Z).
/// O código é normalizado em maiúsculas. A validação semântica (existência do código)
/// pode ser feita pela integração com a API da OMS (ICD API) - ver ICidLookupService.
/// </summary>
public static partial class CidCode
{
    // CID-10: letra + 2 dígitos + subcategoria opcional (1 dígito/caractere).
    [GeneratedRegex(@"^[A-Z][0-9]{2}(\.[0-9A-Z]{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex Icd10();

    // CID-11: 4 caracteres alfanuméricos (com ao menos uma letra) + extensões opcionais após ponto.
    [GeneratedRegex(@"^[0-9A-Z][A-Z][0-9A-Z]{2}(\.[0-9A-Z]{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex Icd11();

    public static string Normalize(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsValid(string? code)
    {
        var normalized = Normalize(code);
        return Icd10().IsMatch(normalized) || Icd11().IsMatch(normalized);
    }
}
