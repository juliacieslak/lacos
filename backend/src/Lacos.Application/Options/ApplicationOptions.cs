namespace Lacos.Application.Options;

/// <summary>Versões vigentes dos documentos LGPD (RNF01). Alterar a versão exige novo aceite dos usuários.</summary>
public sealed class LgpdOptions
{
    public const string SectionName = "Lgpd";
    public string TermsOfUseVersion { get; set; } = "1.0";
    public string SensitiveDataConsentVersion { get; set; } = "1.0";
}

/// <summary>Regras de convites para a rede de cuidado.</summary>
public sealed class InvitationOptions
{
    public const string SectionName = "Invitations";
    /// <summary>SUPOSIÇÃO: validade de 7 dias.</summary>
    public int ExpirationDays { get; set; } = 7;
    /// <summary>URL do front-end incluída no e-mail de convite.</summary>
    public string AcceptUrl { get; set; } = "https://app.lacos.com.br/convites";
}

/// <summary>Regras de anexos (RNF07).</summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Documents";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>Parâmetros dos lembretes automáticos (RF12 / RNF11).</summary>
public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";
    /// <summary>Antecedência do lembrete de medicamento.</summary>
    public int MedicationLeadMinutes { get; set; } = 15;
    /// <summary>Tolerância após o horário programado para considerar a dose como não administrada.</summary>
    public int MedicationMissedToleranceMinutes { get; set; } = 60;
    /// <summary>Antecedências dos lembretes de compromissos da agenda.</summary>
    public int[] AppointmentReminderLeadMinutes { get; set; } = { 1440, 60 };
    /// <summary>Antecedência do lembrete de rotina de cuidado/aferição.</summary>
    public int CareRoutineLeadMinutes { get; set; } = 15;
}

/// <summary>Canais externos de entrega das notificações (a caixa de entrada in-app está sempre ativa).</summary>
public sealed class NotificationChannelOptions
{
    public const string SectionName = "NotificationChannels";
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    /// <summary>Quantidade de notificações processadas por rodada do worker.</summary>
    public int BatchSize { get; set; } = 100;
}
