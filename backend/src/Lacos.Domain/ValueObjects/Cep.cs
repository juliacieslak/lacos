namespace Lacos.Domain.ValueObjects;

/// <summary>
/// Objeto de valor para CEP (Código de Endereçamento Postal), exigido em RF01 e RF03.
/// Regra: 8 dígitos numéricos, aceitando a máscara 00000-000 na entrada.
/// A existência real do CEP pode ser verificada pela integração externa (ViaCEP).
/// </summary>
public sealed record Cep
{
    private Cep(string value) => Value = value;

    /// <summary>CEP normalizado (8 dígitos).</summary>
    public string Value { get; }

    public static bool TryCreate(string? input, out Cep? cep)
    {
        cep = null;
        var digits = input is null ? string.Empty : new string(input.Where(char.IsDigit).ToArray());
        // Rejeita entradas com caracteres inesperados além da máscara (ex.: letras).
        if (input is not null && input.Any(c => !char.IsDigit(c) && c != '-' && c != '.' && c != ' ')) return false;
        if (digits.Length != 8 || digits == "00000000") return false;
        cep = new Cep(digits);
        return true;
    }

    public static Cep Create(string? input) =>
        TryCreate(input, out var cep) ? cep! : throw new ArgumentException("CEP inválido.", nameof(input));

    public static bool IsValid(string? input) => TryCreate(input, out _);

    public string Formatted => $"{Value[..5]}-{Value[5..]}";

    public override string ToString() => Formatted;
}
