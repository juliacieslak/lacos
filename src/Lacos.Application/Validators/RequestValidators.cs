using FluentValidation;
using Lacos.Application.DTOs;
using Lacos.Domain.Enums;

namespace Lacos.Application.Validators;

/// <summary>RF01: todos os dados cadastrais são obrigatórios; aceite LGPD obrigatório (RNF01).</summary>
public sealed class CompleteOnboardingRequestValidator : AbstractValidator<CompleteOnboardingRequest>
{
    public CompleteOnboardingRequestValidator()
    {
        RuleFor(x => x.ProfileType).IsInEnum();
        RuleFor(x => x.Name).RequiredText(150);
        RuleFor(x => x.Cpf).ValidCpf();
        RuleFor(x => x.BirthDate).ValidBirthDate();
        RuleFor(x => x.Phone).NotEmpty().ValidPhone();
        RuleFor(x => x.Profession).RequiredText(100);
        RuleFor(x => x.Cep).ValidCep();
        RuleFor(x => x.Ethnicity).IsInEnum();
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.AcceptTermsOfUse).Equal(true).WithMessage("É necessário aceitar os termos de uso.");
        RuleFor(x => x.AcceptSensitiveDataProcessing).Equal(true).WithMessage("É necessário consentir com o tratamento de dados sensíveis de saúde.");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.ProfileType).IsInEnum();
        RuleFor(x => x.Name).RequiredText(150);
        RuleFor(x => x.BirthDate).ValidBirthDate();
        RuleFor(x => x.Phone).NotEmpty().ValidPhone();
        RuleFor(x => x.Profession).RequiredText(100);
        RuleFor(x => x.Cep).ValidCep();
        RuleFor(x => x.Ethnicity).IsInEnum();
        RuleFor(x => x.Gender).IsInEnum();
    }
}

public sealed class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Confirmation).Equal("EXCLUIR").WithMessage("Digite EXCLUIR para confirmar.");
        RuleFor(x => x.Reason).OptionalText(500);
    }
}

/// <summary>
/// RF03: nome completo, data e local de nascimento, CEP, etnia, CPF, gênero, altura e relação com o administrador.
/// SUPOSIÇÃO: e-mail e telefone do paciente são opcionais (pacientes idosos/dependentes podem não possuí-los).
/// </summary>
public sealed class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FullName).RequiredText(150);
        RuleFor(x => x.BirthDate).ValidBirthDate();
        RuleFor(x => x.BirthPlace).RequiredText(150);
        RuleFor(x => x.Cep).ValidCep();
        RuleFor(x => x.Ethnicity).IsInEnum();
        RuleFor(x => x.Cpf).ValidCpf();
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).ValidPhone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.HeightCm).InclusiveBetween(30, 250).WithMessage("Altura deve estar entre 30 e 250 cm.");
        RuleFor(x => x.RelationshipType).RequiredText(50);
    }
}

public sealed class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.FullName).RequiredText(150);
        RuleFor(x => x.BirthDate).ValidBirthDate();
        RuleFor(x => x.BirthPlace).RequiredText(150);
        RuleFor(x => x.Cep).ValidCep();
        RuleFor(x => x.Ethnicity).IsInEnum();
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).ValidPhone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.HeightCm).InclusiveBetween(30, 250);
    }
}

public sealed class ErasePatientRequestValidator : AbstractValidator<ErasePatientRequest>
{
    public ErasePatientRequestValidator()
    {
        RuleFor(x => x.ConfirmationCode).NotEmpty();
        RuleFor(x => x.Reason).RequiredText(500);
    }
}

public sealed class InviteMemberRequestValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.RelationshipType).RequiredText(50);
        RuleFor(x => x.Preset).IsInEnum();
        RuleFor(x => x.Permissions).NotNull().When(x => x.Preset == AccessProfilePreset.Custom)
            .WithMessage("Informe as permissões para o perfil personalizado.");
    }
}

public sealed class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.RelationshipType).RequiredText(50);
        RuleFor(x => x.Preset).IsInEnum();
        RuleFor(x => x.Permissions).NotNull().When(x => x.Preset == AccessProfilePreset.Custom);
    }
}

/// <summary>RF08: nome, dosagem, via, data de início, frequência e horários.</summary>
public sealed class MedicationRequestValidator : AbstractValidator<MedicationRequest>
{
    public MedicationRequestValidator()
    {
        RuleFor(x => x.Name).RequiredText(150);
        RuleFor(x => x.Dosage).RequiredText(100);
        RuleFor(x => x.Brand).OptionalText(100);
        RuleFor(x => x.UsageReason).OptionalText(500);
        RuleFor(x => x.AdministrationRoute).IsInEnum();
        RuleFor(x => x.PrescribedBy).OptionalText(150);
        RuleFor(x => x.Frequency).RequiredText(100);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue)
            .WithMessage("A data de fim não pode ser anterior à de início.");
        RuleFor(x => x.AdministrationTimes).Must(t => t is null || t.Count <= 24).WithMessage("Máximo de 24 horários por dia.");
        RuleForEach(x => x.DaysOfWeek).IsInEnum();
    }
}

public sealed class MedicationAdministrationRequestValidator : AbstractValidator<MedicationAdministrationRequest>
{
    public MedicationAdministrationRequestValidator()
    {
        RuleFor(x => x.MedicationId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Notes).NotEmpty().When(x => x.Status == MedicationAdministrationStatus.NotAdministered)
            .WithMessage("Informe o motivo da não administração.");
    }
}

/// <summary>RF09 com faixas de plausibilidade fisiológica para evitar erros de digitação.</summary>
public sealed class HealthMeasurementRequestValidator : AbstractValidator<HealthMeasurementRequest>
{
    public HealthMeasurementRequestValidator()
    {
        RuleFor(x => x.Systolic).InclusiveBetween((short)40, (short)300).When(x => x.Systolic.HasValue);
        RuleFor(x => x.Diastolic).InclusiveBetween((short)20, (short)200).When(x => x.Diastolic.HasValue);
        RuleFor(x => x.HeartRate).InclusiveBetween((short)20, (short)250).When(x => x.HeartRate.HasValue);
        RuleFor(x => x.Temperature).InclusiveBetween(25m, 45m).When(x => x.Temperature.HasValue && x.TemperatureUnit != TemperatureUnit.Fahrenheit);
        RuleFor(x => x.Temperature).InclusiveBetween(77m, 113m).When(x => x.Temperature.HasValue && x.TemperatureUnit == TemperatureUnit.Fahrenheit);
        RuleFor(x => x.RespiratoryRate).InclusiveBetween((short)4, (short)80).When(x => x.RespiratoryRate.HasValue);
        RuleFor(x => x.OxygenSaturation).InclusiveBetween((short)50, (short)100).When(x => x.OxygenSaturation.HasValue);
        RuleFor(x => x.BloodGlucose).InclusiveBetween(0.5m, 1000m).When(x => x.BloodGlucose.HasValue);
        RuleFor(x => x.PainLevel).InclusiveBetween((short)0, (short)10).When(x => x.PainLevel.HasValue);
        RuleFor(x => x.Weight).InclusiveBetween(0.5m, 1000m).When(x => x.Weight.HasValue);
        RuleFor(x => x.Description).OptionalText(2000);
    }
}

/// <summary>RF13: limites dentro das faixas fisiológicas e compatíveis com os tipos smallint/numeric do modelo.</summary>
public sealed class HealthThresholdRequestValidator : AbstractValidator<HealthThresholdRequest>
{
    public HealthThresholdRequestValidator()
    {
        Range(x => x.SystolicPressure, 40, 300, "Pressão sistólica");
        Range(x => x.DiastolicPressure, 20, 200, "Pressão diastólica");
        Range(x => x.HeartRate, 20, 250, "Frequência cardíaca");
        Range(x => x.Temperature, 25, 45, "Temperatura (°C)");
        Range(x => x.RespiratoryRate, 4, 80, "Frequência respiratória");
        Range(x => x.OxygenSaturation, 50, 100, "Saturação");
        Range(x => x.BloodGlucose, 10, 1000, "Glicemia (mg/dL)");
    }

    private void Range(System.Linq.Expressions.Expression<Func<HealthThresholdRequest, MetricLimitDto?>> selector, decimal min, decimal max, string name)
    {
        RuleFor(selector).Must(l => l is null || ((l.Min is null || (l.Min >= min && l.Min <= max)) && (l.Max is null || (l.Max >= min && l.Max <= max))))
            .WithMessage($"{name}: os limites devem estar entre {min} e {max}.");
        RuleFor(selector).Must(l => l is null || l.Min is null || l.Max is null || l.Min < l.Max)
            .WithMessage($"{name}: o limite mínimo deve ser menor que o máximo.");
    }
}

public sealed class SymptomRequestValidator : AbstractValidator<SymptomRequest>
{
    public SymptomRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).RequiredText(2000);
    }
}

public sealed class CareDiaryRequestValidator : AbstractValidator<CareDiaryRequest>
{
    public CareDiaryRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).RequiredText(4000);
    }
}

public sealed class HealthConditionRequestValidator : AbstractValidator<HealthConditionRequest>
{
    public HealthConditionRequestValidator()
    {
        RuleFor(x => x.Name).RequiredText(200);
        RuleFor(x => x.CidCode).NotEmpty().Must(Lacos.Domain.ValueObjects.CidCode.IsValid).WithMessage("Código CID inválido.");
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).OptionalText(2000);
    }
}

public sealed class SurgeryRequestValidator : AbstractValidator<SurgeryRequest>
{
    public SurgeryRequestValidator()
    {
        RuleFor(x => x.ProcedureName).RequiredText(200);
        RuleFor(x => x.Reason).RequiredText(1000);
    }
}

public sealed class AllergyRequestValidator : AbstractValidator<AllergyRequest>
{
    public AllergyRequestValidator()
    {
        RuleFor(x => x.CausativeAgent).RequiredText(200);
        RuleFor(x => x.Manifestation).RequiredText(1000);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

/// <summary>RF18: tipo, descrição, data e hora, duração, local, profissional e anotações.</summary>
public sealed class PatientEventRequestValidator : AbstractValidator<PatientEventRequest>
{
    public PatientEventRequestValidator()
    {
        RuleFor(x => x.EventType).IsInEnum();
        RuleFor(x => x.Title).RequiredText(150);
        RuleFor(x => x.ScheduledAt).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 1440).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.LocationName).OptionalText(200);
        RuleFor(x => x.ProviderName).OptionalText(150);
        RuleFor(x => x.Notes).OptionalText(4000);
    }
}

public sealed class ChangeEventStatusRequestValidator : AbstractValidator<ChangeEventStatusRequest>
{
    public ChangeEventStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Summary).OptionalText(4000);
    }
}

public sealed class CareRoutineTaskRequestValidator : AbstractValidator<CareRoutineTaskRequest>
{
    public CareRoutineTaskRequestValidator()
    {
        RuleFor(x => x.Title).RequiredText(150);
        RuleFor(x => x.Description).OptionalText(1000);
        RuleFor(x => x.Category).IsInEnum();
        RuleForEach(x => x.DaysOfWeek).IsInEnum();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue && x.StartDate.HasValue);
    }
}

public sealed class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).OptionalText(1000);
    }
}
