namespace Lacos.Domain.ValueObjects;

/// <summary>
/// Objeto de valor para CPF (Cadastro de Pessoas Físicas), exigido no cadastro de usuário (RF01) e de paciente (RF03).
///
/// Algoritmo de validação (Receita Federal):
/// 1. Remove a máscara e exige 11 dígitos.
/// 2. Rejeita sequências de dígitos iguais (ex.: 111.111.111-11), que passam no cálculo mas são inválidas.
/// 3. Primeiro dígito verificador: soma dos 9 primeiros dígitos multiplicados pelos pesos 10..2;
///    resto = (soma * 10) % 11; se resto == 10, o dígito é 0.
/// 4. Segundo dígito verificador: mesma regra com os 10 primeiros dígitos e pesos 11..2.
/// </summary>
public sealed record Cpf
{
    private Cpf(string value) => Value = value;

    /// <summary>CPF normalizado (somente dígitos).</summary>
    public string Value { get; }

    /// <summary>Cria um CPF validado. Lança <see cref="ArgumentException"/> se for inválido.</summary>
    public static Cpf Create(string? input)
    {
        if (!TryCreate(input, out var cpf)) throw new ArgumentException("CPF inválido.", nameof(input));
        return cpf!;
    }

    /// <summary>Tenta criar um CPF validado sem lançar exceção.</summary>
    public static bool TryCreate(string? input, out Cpf? cpf)
    {
        cpf = null;
        var digits = Normalize(input);
        if (digits.Length != 11) return false;
        if (digits.All(c => c == digits[0])) return false;

        if (CalculateDigit(digits, 9) != digits[9] - '0') return false;
        if (CalculateDigit(digits, 10) != digits[10] - '0') return false;

        cpf = new Cpf(digits);
        return true;
    }

    /// <summary>Indica se o texto representa um CPF válido.</summary>
    public static bool IsValid(string? input) => TryCreate(input, out _);

    /// <summary>Remove qualquer caractere não numérico.</summary>
    public static string Normalize(string? input) =>
        input is null ? string.Empty : new string(input.Where(char.IsDigit).ToArray());

    // Calcula o dígito verificador considerando os "length" primeiros dígitos.
    private static int CalculateDigit(string digits, int length)
    {
        var sum = 0;
        var weight = length + 1;
        for (var i = 0; i < length; i++)
            sum += (digits[i] - '0') * weight--;
        var rest = sum * 10 % 11;
        return rest == 10 ? 0 : rest;
    }

    /// <summary>Formato mascarado para exibição: 000.000.000-00.</summary>
    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    /// <summary>Versão parcialmente oculta para exibição segura: ***.456.789-**.</summary>
    public string Masked => $"***.{Value[3..6]}.{Value[6..9]}-**";

    public override string ToString() => Masked;
}
