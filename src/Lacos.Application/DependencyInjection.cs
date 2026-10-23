using FluentValidation;
using Lacos.Application.Options;
using Lacos.Application.Services;
using Lacos.Application.Services.Clinical;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lacos.Application;

/// <summary>
/// Registro dos serviços da camada de aplicação no contêiner de injeção de dependência.
/// Todos os serviços são "scoped" (uma instância por requisição HTTP), pois dependem do usuário atual e do DbContext.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LgpdOptions>(configuration.GetSection(LgpdOptions.SectionName));
        services.Configure<InvitationOptions>(configuration.GetSection(InvitationOptions.SectionName));
        services.Configure<DocumentOptions>(configuration.GetSection(DocumentOptions.SectionName));
        services.Configure<ReminderOptions>(configuration.GetSection(ReminderOptions.SectionName));
        services.Configure<NotificationChannelOptions>(configuration.GetSection(NotificationChannelOptions.SectionName));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Serviços transversais.
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IPatientAccessService, PatientAccessService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<PatientRecordDependencies>();
        services.AddScoped<IAgendaBuilder, AgendaBuilder>();

        // Casos de uso.
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ICareTeamService, CareTeamService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<ITimelineService, TimelineService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IPatientDocumentService, PatientDocumentService>();
        services.AddScoped<IAgendaService, AgendaService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<ICareReminderService, CareReminderService>();
        services.AddScoped<INotificationDispatchService, NotificationDispatchService>();

        // Registros clínicos (concretos: cada controller depende do serviço específico).
        services.AddScoped<MedicationService>();
        services.AddScoped<MedicationAdministrationService>();
        services.AddScoped<HealthMeasurementService>();
        services.AddScoped<HealthThresholdService>();
        services.AddScoped<SymptomService>();
        services.AddScoped<CareDiaryService>();
        services.AddScoped<HealthConditionService>();
        services.AddScoped<SurgeryService>();
        services.AddScoped<AllergyService>();
        services.AddScoped<PatientEventService>();
        services.AddScoped<CareRoutineService>();

        return services;
    }
}
