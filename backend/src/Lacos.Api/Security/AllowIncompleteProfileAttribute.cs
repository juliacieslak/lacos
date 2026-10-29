namespace Lacos.Api.Security;

/// <summary>
/// Permite acessar o endpoint antes de concluir o onboarding e o aceite LGPD
/// (ex.: início de sessão, onboarding, consentimentos, convites pendentes).
/// Sem este atributo, o filtro <see cref="Filters.ActiveUserFilter"/> exige cadastro completo e consentimentos vigentes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowIncompleteProfileAttribute : Attribute
{
}
