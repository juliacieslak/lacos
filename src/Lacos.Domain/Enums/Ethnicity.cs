namespace Lacos.Domain.Enums;

/// <summary>
/// Etnia (cor/raça) informada no cadastro de usuário (RF01) e de paciente (RF03).
/// SUPOSIÇÃO: adotada a classificação do IBGE, padrão oficial no Brasil.
/// </summary>
public enum Ethnicity
{
    Branca,
    Preta,
    Parda,
    Amarela,
    Indigena,
    NaoDeclarada
}
