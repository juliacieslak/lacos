using Lacos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lacos.Infrastructure.Persistence.Configurations;

/// <summary>Tabela health_thresholds — exatamente as colunas do modelo de dados (RF13).</summary>
public sealed class HealthThresholdConfiguration : BaseEntityConfiguration<HealthThreshold>
{
    protected override void ConfigureEntity(EntityTypeBuilder<HealthThreshold> b)
    {
        b.ToTable("health_thresholds");
        b.HasOne<Patient>().WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.PatientId).IsUnique(); // Uma configuração ativa por paciente.
        b.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.HeartBpmMinLimit).HasColumnName("heart_bpm_min_limit");
        b.Property(e => e.HeartBpmMinNotice).HasColumnName("heart_bpm_min_notice");
        b.Property(e => e.HeartBpmMaxLimit).HasColumnName("heart_bpm_max_limit");
        b.Property(e => e.HeartBpmMaxNotice).HasColumnName("heart_bpm_max_notice");
        b.Property(e => e.TemperatureMinLimit).HasColumnType("numeric(4,1)");
        b.Property(e => e.TemperatureMaxLimit).HasColumnType("numeric(4,1)");
        b.Property(e => e.BloodGlucoseMinLimit).HasColumnType("numeric(6,2)");
        b.Property(e => e.BloodGlucoseMaxLimit).HasColumnType("numeric(6,2)");
        foreach (var notice in new[] { "SystolicMinNotice", "SystolicMaxNotice", "DiastolicMinNotice", "DiastolicMaxNotice",
                     "HeartBpmMinNotice", "HeartBpmMaxNotice", "TemperatureMinNotice", "TemperatureMaxNotice",
                     "RespiratoryRateMinNotice", "RespiratoryRateMaxNotice", "OxygenSaturationMinNotice", "OxygenSaturationMaxNotice",
                     "BloodGlucoseMinNotice", "BloodGlucoseMaxNotice" })
            b.Property<bool>(notice).HasDefaultValue(false);
    }
}

/// <summary>Tabela patient_events — exatamente as colunas do modelo de dados (RF18) + exclusão lógica.</summary>
public sealed class PatientEventConfiguration : PatientRecordConfiguration<PatientEvent>
{
    protected override void ConfigureRecord(EntityTypeBuilder<PatientEvent> b)
    {
        b.ToTable("patient_events");
        b.Property(e => e.EventType).HasMaxLength(30);
        b.Property(e => e.Title).HasMaxLength(150).IsRequired();
        b.Property(e => e.LocationName).HasMaxLength(200);
        b.Property(e => e.ProviderName).HasMaxLength(150);
        b.Property(e => e.Status).HasMaxLength(30);
        b.HasIndex(e => new { e.PatientId, e.ScheduledAt });
        b.HasIndex(e => new { e.Status, e.ScheduledAt });
        b.ToTable(t => t.HasCheckConstraint("ck_patient_events_duration", "duration_minutes IS NULL OR duration_minutes > 0"));
    }
}

/// <summary>Tabela medications (RF08). Horários em time[] e dias da semana em integer[] (PostgreSQL arrays).</summary>
public sealed class MedicationConfiguration : PatientRecordConfiguration<Medication>
{
    protected override void ConfigureRecord(EntityTypeBuilder<Medication> b)
    {
        b.ToTable("medications");
        b.Property(e => e.Name).HasMaxLength(150).IsRequired();
        b.Property(e => e.Dosage).HasMaxLength(100).IsRequired();
        b.Property(e => e.Brand).HasMaxLength(100);
        b.Property(e => e.PrescribedBy).HasMaxLength(150);
        b.Property(e => e.Frequency).HasMaxLength(100).IsRequired();
        b.HasIndex(e => new { e.PatientId, e.StartDate, e.EndDate });
    }
}

/// <summary>Tabela medication_administrations: uma dose programada só pode ser registrada uma vez.</summary>
public sealed class MedicationAdministrationConfiguration : PatientRecordConfiguration<MedicationAdministration>
{
    protected override void ConfigureRecord(EntityTypeBuilder<MedicationAdministration> b)
    {
        b.ToTable("medication_administrations");
        b.HasOne(e => e.Medication).WithMany().HasForeignKey(e => e.MedicationId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.MedicationId, e.ScheduledFor }).IsUnique()
            .HasFilter("scheduled_for IS NOT NULL AND deleted_at IS NULL");
        b.HasIndex(e => new { e.PatientId, e.AdministeredAt });
    }
}

/// <summary>Tabela health_measurements (RF09).</summary>
public sealed class HealthMeasurementConfiguration : PatientRecordConfiguration<HealthMeasurement>
{
    protected override void ConfigureRecord(EntityTypeBuilder<HealthMeasurement> b)
    {
        b.ToTable("health_measurements");
        b.Property(e => e.Temperature).HasColumnType("numeric(4,1)");
        b.Property(e => e.BloodGlucose).HasColumnType("numeric(6,2)");
        b.Property(e => e.Weight).HasColumnType("numeric(6,2)");
        b.HasIndex(e => new { e.PatientId, e.MeasuredAt });
        b.ToTable(t => t.HasCheckConstraint("ck_health_measurements_pain", "pain_level IS NULL OR (pain_level BETWEEN 0 AND 10)"));
    }
}

/// <summary>Tabela symptom_records (RF09).</summary>
public sealed class SymptomRecordConfiguration : PatientRecordConfiguration<SymptomRecord>
{
    protected override void ConfigureRecord(EntityTypeBuilder<SymptomRecord> b)
    {
        b.ToTable("symptom_records");
        b.HasIndex(e => new { e.PatientId, e.OccurredAt });
    }
}

/// <summary>Tabela care_diary_entries (RF04).</summary>
public sealed class CareDiaryEntryConfiguration : PatientRecordConfiguration<CareDiaryEntry>
{
    protected override void ConfigureRecord(EntityTypeBuilder<CareDiaryEntry> b)
    {
        b.ToTable("care_diary_entries");
        b.HasIndex(e => new { e.PatientId, e.OccurredAt });
    }
}

/// <summary>Tabela care_routine_tasks (rotina de cuidado).</summary>
public sealed class CareRoutineTaskConfiguration : PatientRecordConfiguration<CareRoutineTask>
{
    protected override void ConfigureRecord(EntityTypeBuilder<CareRoutineTask> b)
    {
        b.ToTable("care_routine_tasks");
        b.Property(e => e.Title).HasMaxLength(150).IsRequired();
        b.Property(e => e.Description).HasMaxLength(1000);
    }
}

/// <summary>Tabela care_routine_completions: uma conclusão ativa por tarefa e data.</summary>
public sealed class CareRoutineCompletionConfiguration : PatientRecordConfiguration<CareRoutineCompletion>
{
    protected override void ConfigureRecord(EntityTypeBuilder<CareRoutineCompletion> b)
    {
        b.ToTable("care_routine_completions");
        b.HasOne(e => e.Task).WithMany().HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.TaskId, e.ScheduledDate }).IsUnique().HasFilter("deleted_at IS NULL");
    }
}

/// <summary>Tabela health_conditions (RF14).</summary>
public sealed class HealthConditionConfiguration : PatientRecordConfiguration<HealthCondition>
{
    protected override void ConfigureRecord(EntityTypeBuilder<HealthCondition> b) => b.ToTable("health_conditions");
}

/// <summary>Tabela surgeries (RF15).</summary>
public sealed class SurgeryConfiguration : PatientRecordConfiguration<Surgery>
{
    protected override void ConfigureRecord(EntityTypeBuilder<Surgery> b)
    {
        b.ToTable("surgeries");
        b.Property(e => e.ProcedureName).HasMaxLength(200).IsRequired();
    }
}

/// <summary>Tabela allergies (RF16).</summary>
public sealed class AllergyConfiguration : PatientRecordConfiguration<Allergy>
{
    protected override void ConfigureRecord(EntityTypeBuilder<Allergy> b)
    {
        b.ToTable("allergies");
        b.Property(e => e.CausativeAgent).HasMaxLength(200).IsRequired();
    }
}

/// <summary>Tabela patient_documents (RF19).</summary>
public sealed class PatientDocumentConfiguration : PatientRecordConfiguration<PatientDocument>
{
    protected override void ConfigureRecord(EntityTypeBuilder<PatientDocument> b)
    {
        b.ToTable("patient_documents");
        b.Property(e => e.FileName).HasMaxLength(200).IsRequired();
        b.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
        b.Property(e => e.Sha256).HasMaxLength(64).IsRequired();
        b.Property(e => e.StoragePath).HasMaxLength(300).IsRequired();
        b.HasOne<PatientEvent>().WithMany().HasForeignKey(e => e.RelatedEventId).OnDelete(DeleteBehavior.SetNull);
    }
}
