using System.Text.RegularExpressions;
using FluentValidation;
using Lacos.Domain.ValueObjects;

namespace Lacos.Application.Validators;

/// <summary>
/// Regras de validação reutilizáveis (DRY) aplicadas pelos validadores de requisição.
/// Os limites de tamanho seguem os tipos do modelo de dados (ex.: name varchar(150), phone varchar(30)).
/// </summary>
public static partial class ValidationRules
{
    [GeneratedRegex(@"^\+?[0-9\s\-\(\)]{10,20}$")]
    private static partial Regex PhoneRegex();

    /// <summary>Telefone: 10 a 13 dígitos (DDD + número, com DDI opcional), aceitando máscara.</summary>
    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(p => p is null || (PhoneRegex().IsMatch(p) && p.Count(char.IsDigit) is >= 10 and <= 13))
            .WithMessage("Telefone inválido. Informe DDD + número.");

    public static IRuleBuilderOptions<T, string> ValidCpf<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("CPF é obrigatório.").Must(Cpf.IsValid).WithMessage("CPF inválido.");

    public static IRuleBuilderOptions<T, string> ValidCep<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("CEP é obrigatório.").Must(Cep.IsValid).WithMessage("CEP inválido.");

    /// <summary>Data de nascimento: não futura e não anterior a 1900.</summary>
    public static IRuleBuilderOptions<T, DateOnly> ValidBirthDate<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(14)) && d.Year >= 1900)
            .WithMessage("Data de nascimento inválida.");

    public static IRuleBuilderOptions<T, string> RequiredText<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().WithMessage("Campo obrigatório.").MaximumLength(maxLength).WithMessage($"Máximo de {maxLength} caracteres.");

    public static IRuleBuilderOptions<T, string?> OptionalText<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.MaximumLength(maxLength).WithMessage($"Máximo de {maxLength} caracteres.");
}
