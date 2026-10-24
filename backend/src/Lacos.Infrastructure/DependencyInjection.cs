using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Entities;
using Lacos.Domain.Interfaces;
using Lacos.Infrastructure.BackgroundJobs;
using Lacos.Infrastructure.Integrations.ExternalApis.Auth0;
using Lacos.Infrastructure.Integrations.ExternalApis.Common;
using Lacos.Infrastructure.Integrations.ExternalApis.Nominatim;
using Lacos.Infrastructure.Integrations.ExternalApis.Overpass;
using Lacos.Infrastructure.Integrations.ExternalApis.Email;
using Lacos.Infrastructure.Integrations.ExternalApis.Push;
using Lacos.Infrastructure.Integrations.ExternalApis.Storage;
using Lacos.Infrastructure.Integrations.ExternalApis.ViaCep;
using Lacos.Infrastructure.Integrations.ExternalApis.WhoIcd;
using Lacos.Infrastructure.Persistence;
using Lacos.Infrastructure.Persistence.Context;
using Lacos.Infrastructure.Persistence.Interceptors;
using Lacos.Infrastructure.Persistence.Repositories;
using Lacos.Infrastructure.Security;
using Lacos.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure;

/// <summary>
/// Registro da camada de infraestrutura: banco (Supabase/PostgreSQL), repositórios, segurança, integrações e workers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
        services.Configure<SchedulingOptions>(configuration.GetSection(SchedulingOptions.SectionName));
        services.Configure<BackgroundJobOptions>(configuration.GetSection(BackgroundJobOptions.SectionName));

        // Segurança (singletons: chaves imutáveis durante a execução).
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
        services.AddSingleton<IBlindIndexService, HmacBlindIndexService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddPersistence(configuration);
        services.AddExternalIntegrations(configuration);

        // Notificações em tempo (quase) real - RNF11.
        services.AddSingleton<NotificationDispatchSignal>();
        services.AddSingleton<INotificationDispatchSignal>(sp => sp.GetRequiredService<NotificationDispatchSignal>());
        if (configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            services.AddHostedService<CareReminderWorker>();
            services.AddHostedService<NotificationDispatchWorker>();
        }
        return services;
    }

    /// <summary>
    /// Camada de conexão EF Core + Npgsql com o PostgreSQL do Supabase (Frontend → Backend → Supabase).
    /// - A estrutura do banco é criada MANUALMENTE no Supabase: não há migrations, EnsureCreated nem scripts de schema.
    /// - A connection string vem de configuração/variável de ambiente (ConnectionStrings__Supabase).
    ///   Se ainda não estiver configurada, a API inicia normalmente e as operações que dependem do banco
    ///   respondem 503 DATABASE_NOT_CONFIGURED (ver <see cref="DatabaseConfigurationGuardInterceptor"/>).
    /// - Nomes em snake_case (padrão do modelo de dados e do PostgreSQL).
    /// - Retentativa automática em falhas transitórias de rede (RNF08 - disponibilidade).
    /// - Interceptadores de carimbo de data e de auditoria.
    /// </summary>
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TimestampInterceptor>();
        services.AddScoped<AuditTrailInterceptor>();
        services.AddSingleton<DatabaseConfigurationGuardInterceptor>();

        services.AddDbContext<LacosDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Supabase");
            void ConfigureNpgsql(Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder npgsql)
            {
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                npgsql.CommandTimeout(30);
            }

            if (string.IsNullOrWhiteSpace(connectionString)) options.UseNpgsql(ConfigureNpgsql);
            else options.UseNpgsql(connectionString, ConfigureNpgsql);

            options.UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<DatabaseConfigurationGuardInterceptor>(),
                    sp.GetRequiredService<TimestampInterceptor>(), sp.GetRequiredService<AuditTrailInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LacosDbContext>());
        services.AddScoped<IPublicCodeGenerator, PostgresPublicCodeGenerator>();
        services.AddScoped<IPatientDataEraser, PatientDataEraser>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientPermissionRepository, PatientPermissionRepository>();
        services.AddScoped<IPatientInvitationRepository, PatientInvitationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IHealthThresholdRepository, HealthThresholdRepository>();

        services.AddScoped<PatientEventRepository>();
        services.AddScoped<IPatientEventRepository>(sp => sp.GetRequiredService<PatientEventRepository>());
        services.AddScoped<IPatientRecordRepository<PatientEvent>>(sp => sp.GetRequiredService<PatientEventRepository>());
        services.AddScoped<MedicationRepository>();
        services.AddScoped<IMedicationRepository>(sp => sp.GetRequiredService<MedicationRepository>());
        services.AddScoped<IPatientRecordRepository<Medication>>(sp => sp.GetRequiredService<MedicationRepository>());
        services.AddScoped<MedicationAdministrationRepository>();
        services.AddScoped<IMedicationAdministrationRepository>(sp => sp.GetRequiredService<MedicationAdministrationRepository>());
        services.AddScoped<IPatientRecordRepository<MedicationAdministration>>(sp => sp.GetRequiredService<MedicationAdministrationRepository>());
        services.AddScoped<HealthMeasurementRepository>();
        services.AddScoped<IHealthMeasurementRepository>(sp => sp.GetRequiredService<HealthMeasurementRepository>());
        services.AddScoped<IPatientRecordRepository<HealthMeasurement>>(sp => sp.GetRequiredService<HealthMeasurementRepository>());
        services.AddScoped<HealthConditionRepository>();
        services.AddScoped<IHealthConditionRepository>(sp => sp.GetRequiredService<HealthConditionRepository>());
        services.AddScoped<IPatientRecordRepository<HealthCondition>>(sp => sp.GetRequiredService<HealthConditionRepository>());
        services.AddScoped<CareRoutineRepository>();
        services.AddScoped<ICareRoutineRepository>(sp => sp.GetRequiredService<CareRoutineRepository>());
        services.AddScoped<IPatientRecordRepository<CareRoutineTask>>(sp => sp.GetRequiredService<CareRoutineRepository>());
        services.AddScoped<IPatientRecordRepository<SymptomRecord>, SymptomRecordRepository>();
        services.AddScoped<IPatientRecordRepository<CareDiaryEntry>, CareDiaryRepository>();
        services.AddScoped<IPatientRecordRepository<Surgery>, PatientRecordRepository<Surgery>>();
        services.AddScoped<IPatientRecordRepository<Allergy>, PatientRecordRepository<Allergy>>();
        services.AddScoped<IPatientRecordRepository<PatientDocument>, PatientRecordRepository<PatientDocument>>();
    }

    /// <summary>
    /// Integrações externas com HttpClient tipado + resiliência padrão (retry, circuit breaker, timeout).
    /// A implementação de cada contrato é escolhida por configuração, com fallback seguro.
    /// </summary>
    private static void AddExternalIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<ViaCepOptions>(configuration.GetSection(ViaCepOptions.SectionName));
        services.Configure<WhoIcdOptions>(configuration.GetSection(WhoIcdOptions.SectionName));
        services.Configure<Auth0Options>(configuration.GetSection(Auth0Options.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<PushOptions>(configuration.GetSection(PushOptions.SectionName));

        // Storage de arquivos.
        if (string.Equals(configuration["Integrations:Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IFileStorage, SupabaseFileStorage>().AddStandardResilienceHandler();
        else
            services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddSingleton<CachedRequestCoalescer>();
        services.Configure<NominatimOptions>(configuration.GetSection(NominatimOptions.SectionName));
        services.Configure<OverpassOptions>(configuration.GetSection(OverpassOptions.SectionName));

        // CEP (ViaCEP): validação e endereço do CEP.
        services.AddHttpClient<IAddressLookupService, ViaCepAddressLookupService>((sp, http) =>
        {
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ViaCepOptions>>().Value.BaseUrl);
        }).AddStandardResilienceHandler(o =>
        {
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
        });

        // Geocodificação (Nominatim): coordenadas aproximadas do CEP. Poucas retentativas para respeitar o limite de 1 req/s.
        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<NominatimOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl);
            http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");
        }).AddStandardResilienceHandler(o =>
        {
            o.Retry.MaxRetryAttempts = 1;
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
        });

        // Estabelecimentos próximos (Overpass/OpenStreetMap): consultas espaciais podem levar vários segundos.
        services.AddHttpClient<INearbyPlacesProvider, OverpassNearbyPlacesProvider>()
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.MaxRetryAttempts = 1;
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(70);
            });

        // CID (OMS).
        if (configuration.GetValue("Integrations:WhoIcd:Enabled", false))
            services.AddHttpClient<ICidLookupService, WhoIcdCidLookupService>().AddStandardResilienceHandler();
        else
            services.AddSingleton<ICidLookupService, UnavailableCidLookupService>();

        // Auth0 Management API.
        services.AddHttpClient<IIdentityProviderService, Auth0ManagementService>().AddStandardResilienceHandler();

        // E-mail.
        if (string.Equals(configuration["Integrations:Email:Provider"], "SendGrid", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IEmailSender, SendGridEmailSender>().AddStandardResilienceHandler();
        else
            services.AddSingleton<IEmailSender, LoggingEmailSender>();

        // Push.
        if (string.Equals(configuration["Integrations:Push:Provider"], "OneSignal", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IPushNotificationSender, OneSignalPushNotificationSender>().AddStandardResilienceHandler();
        else
            services.AddSingleton<IPushNotificationSender, LoggingPushNotificationSender>();
    }
}
