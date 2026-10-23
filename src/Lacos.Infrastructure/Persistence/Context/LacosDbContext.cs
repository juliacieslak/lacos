using System.Linq.Expressions;
using System.Reflection;
using Lacos.Application.Interfaces;
using Lacos.Domain.Common;
using Lacos.Domain.Entities;
using Lacos.Domain.Interfaces;
using Lacos.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lacos.Infrastructure.Persistence.Context;

/// <summary>
/// DbContext do Entity Framework Core para o PostgreSQL do Supabase.
/// Implementa <see cref="IUnitOfWork"/>: um SaveChanges = uma transação, incluindo os registros de auditoria
/// gerados pelo interceptador (garante que nenhuma alteração ocorra sem rastro - RF06/RF10).
/// </summary>
public class LacosDbContext : DbContext, IUnitOfWork
{
    private readonly IEncryptionService _encryption;

    public LacosDbContext(DbContextOptions<LacosDbContext> options, IEncryptionService encryption) : base(options)
    {
        _encryption = encryption;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientUserPermission> PatientUserPermissions => Set<PatientUserPermission>();
    public DbSet<PatientInvitation> PatientInvitations => Set<PatientInvitation>();
    public DbSet<HealthThreshold> HealthThresholds => Set<HealthThreshold>();
    public DbSet<PatientEvent> PatientEvents => Set<PatientEvent>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();
    public DbSet<HealthMeasurement> HealthMeasurements => Set<HealthMeasurement>();
    public DbSet<SymptomRecord> SymptomRecords => Set<SymptomRecord>();
    public DbSet<CareDiaryEntry> CareDiaryEntries => Set<CareDiaryEntry>();
    public DbSet<CareRoutineTask> CareRoutineTasks => Set<CareRoutineTask>();
    public DbSet<CareRoutineCompletion> CareRoutineCompletions => Set<CareRoutineCompletion>();
    public DbSet<HealthCondition> HealthConditions => Set<HealthCondition>();
    public DbSet<Surgery> Surgeries => Set<Surgery>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Sequências usadas para os identificadores públicos "U..." e "P..." (RF17).
        modelBuilder.HasSequence<long>("user_public_code_seq").StartsAt(1).IncrementsBy(1);
        modelBuilder.HasSequence<long>("patient_public_code_seq").StartsAt(1).IncrementsBy(1);

        // Entity Configuration Pattern: cada entidade tem sua classe IEntityTypeConfiguration.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LacosDbContext).Assembly);

        ApplyConventions(modelBuilder);
    }

    /// <summary>
    /// Convenções globais aplicadas por reflexão a todas as entidades (evita repetição):
    /// 1. Enums gravados como texto UPPER_SNAKE_CASE (valores do modelo de dados).
    /// 2. Propriedades [SensitiveData] gravadas criptografadas com AES-256 (RNF02) em colunas "text".
    /// 3. Filtro global de exclusão lógica para registros clínicos (DeletedAt == null).
    /// </summary>
    private void ApplyConventions(ModelBuilder modelBuilder)
    {
        var encrypted = new EncryptedStringConverter(_encryption);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            foreach (var property in entityType.GetProperties())
            {
                var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (propertyType.IsEnum)
                {
                    var converterType = typeof(UpperSnakeEnumConverter<>).MakeGenericType(propertyType);
                    property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
                    if (property.GetMaxLength() is null) property.SetMaxLength(50);
                }

                var member = property.PropertyInfo;
                if (member?.GetCustomAttribute<SensitiveDataAttribute>() is not null && property.ClrType == typeof(string))
                {
                    property.SetValueConverter(encrypted);
                    property.SetMaxLength(null);
                    property.SetColumnType("text");
                }
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType) && entityType.BaseType is null)
            {
                var parameter = Expression.Parameter(clrType, "e");
                var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                var filter = Expression.Lambda(Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?))), parameter);
                entityType.SetQueryFilter(filter);
            }
        }
    }
}
