namespace Lacos.Domain.Enums;

/// <summary>Consentimentos exigidos pela LGPD (RNF01).</summary>
public enum ConsentType
{
    /// <summary>Aceite dos termos de uso.</summary>
    TermsOfUse,
    /// <summary>Consentimento para tratamento de dados sensíveis de saúde (LGPD art. 11).</summary>
    SensitiveHealthData
}
