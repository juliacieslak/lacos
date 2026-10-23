namespace Lacos.Domain.Enums;

/// <summary>
/// Gênero informado no cadastro de usuário (RF01) e de paciente (RF03).
/// SUPOSIÇÃO: a documentação não define os valores possíveis.
/// </summary>
public enum Gender
{
    Female,
    Male,
    NonBinary,
    Other,
    PreferNotToSay
}
