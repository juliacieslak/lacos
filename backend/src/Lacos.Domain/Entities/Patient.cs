using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Perfil do paciente (tabela <c>patients</c>), criado pelo Administrador do Perfil (RF03 / UC03).
///
/// Campos de RF03: nome completo, data e local de nascimento, CEP, etnia, CPF, gênero, e-mail, telefone e altura (cm).
/// O "tipo de relação com o admin do grupo" é armazenado no vínculo de permissão (patient_user_permissions.relationship_type).
/// RF17: <see cref="PublicCode"/> iniciado pela letra "P".
/// RF09: unidades de medida preferidas do paciente.
/// </summary>
public class Patient : BaseEntity, IAuditable
{
    private Patient() { }

    public Patient(string publicCode, Guid createdByUserId, string fullName, DateOnly birthDate, string birthPlace,
        string cep, Ethnicity ethnicity, string cpf, string cpfHash, Gender gender, string? email, string? phone, int heightCm)
    {
        PublicCode = publicCode;
        CreatedByUserId = createdByUserId;
        Cpf = cpf;
        CpfHash = cpfHash;
        CareStatus = PatientCareStatus.InFollowUp;
        TemperatureUnit = TemperatureUnit.Celsius;
        GlucoseUnit = GlucoseUnit.MgPerDl;
        WeightUnit = WeightUnit.Kilogram;
        IsActive = true;
        Update(fullName, birthDate, birthPlace, cep, ethnicity, gender, email, phone, heightCm);
    }

    /// <summary>Identificador legível iniciado por "P" (RF17), ex.: P000045.</summary>
    public string PublicCode { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public DateOnly BirthDate { get; private set; }
    public string BirthPlace { get; private set; } = string.Empty;
    public string Cep { get; private set; } = string.Empty;
    public Ethnicity Ethnicity { get; private set; }

    /// <summary>CPF criptografado (RNF02).</summary>
    [SensitiveData]
    public string? Cpf { get; private set; }

    /// <summary>Blind index do CPF para unicidade.</summary>
    public string? CpfHash { get; private set; }

    public Gender Gender { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public int HeightCm { get; private set; }
    public PatientCareStatus CareStatus { get; private set; }

    public TemperatureUnit TemperatureUnit { get; private set; }
    public GlucoseUnit GlucoseUnit { get; private set; }
    public WeightUnit WeightUnit { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Data da exclusão definitiva (LGPD). Após isso o registro é apenas uma lápide anonimizada.</summary>
    public DateTimeOffset? ErasedAt { get; private set; }

    public string AuditEntityType => "PATIENT";
    public Guid? AuditPatientId => Id;

    /// <summary>Atualiza os dados cadastrais (CPF é imutável para evitar troca de identidade do paciente).</summary>
    public void Update(string fullName, DateOnly birthDate, string birthPlace, string cep, Ethnicity ethnicity,
        Gender gender, string? email, string? phone, int heightCm)
    {
        FullName = fullName.Trim();
        BirthDate = birthDate;
        BirthPlace = birthPlace.Trim();
        Cep = cep;
        Ethnicity = ethnicity;
        Gender = gender;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        HeightCm = heightCm;
    }

    /// <summary>Define as unidades de medida do paciente usadas no registro de métricas (RF09).</summary>
    public void SetMeasurementUnits(TemperatureUnit temperature, GlucoseUnit glucose, WeightUnit weight)
    {
        TemperatureUnit = temperature;
        GlucoseUnit = glucose;
        WeightUnit = weight;
    }

    /// <summary>Altera a situação do acompanhamento (auditado como STATUS_CHANGE).</summary>
    public void ChangeCareStatus(PatientCareStatus status) => CareStatus = status;

    /// <summary>Calcula a idade completa na data de referência (exibida na lista de pacientes - WF02).</summary>
    public int AgeOn(DateOnly referenceDate)
    {
        var age = referenceDate.Year - BirthDate.Year;
        if (BirthDate > referenceDate.AddYears(-age)) age--;
        return age;
    }

    /// <summary>
    /// Exclusão definitiva (LGPD): remove todos os dados pessoais, mantendo somente o identificador
    /// para preservar a integridade da trilha de auditoria imutável.
    /// </summary>
    public void Erase(DateTimeOffset when)
    {
        FullName = "Paciente removido";
        BirthPlace = string.Empty;
        Cep = string.Empty;
        Cpf = null;
        CpfHash = null;
        Email = null;
        Phone = null;
        HeightCm = 0;
        BirthDate = DateOnly.MinValue;
        Ethnicity = Ethnicity.NaoDeclarada;
        Gender = Gender.PreferNotToSay;
        CareStatus = PatientCareStatus.Discharged;
        IsActive = false;
        ErasedAt = when;
    }
}
