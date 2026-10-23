namespace Lacos.Domain.Enums;

/// <summary>
/// Via de administração do medicamento (RF08).
/// SUPOSIÇÃO: lista baseada nas vias usuais de farmacologia; <see cref="Other"/> cobre casos não listados.
/// </summary>
public enum AdministrationRoute
{
    Oral,
    Sublingual,
    Topical,
    Transdermal,
    Inhalation,
    Nasal,
    Ophthalmic,
    Otic,
    Rectal,
    Vaginal,
    Subcutaneous,
    Intramuscular,
    Intravenous,
    Other
}
