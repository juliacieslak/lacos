using FluentAssertions;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.UnitTests.Domain;

public class EntityRuleTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Threshold_alerts_when_value_reaches_or_exceeds_limits()
    {
        var t = new HealthThreshold(PatientId, UserId);
        t.Configure(HealthMetric.SystolicPressure, new MetricLimit(90, true, 140, true), UserId);

        t.Evaluate(HealthMetric.SystolicPressure, 140)!.IsAboveMax.Should().BeTrue();   // atingiu o máximo
        t.Evaluate(HealthMetric.SystolicPressure, 160)!.IsAboveMax.Should().BeTrue();
        t.Evaluate(HealthMetric.SystolicPressure, 90)!.IsAboveMax.Should().BeFalse();    // atingiu o mínimo
        t.Evaluate(HealthMetric.SystolicPressure, 120).Should().BeNull();
    }

    [Fact]
    public void Threshold_does_not_alert_when_notice_disabled()
    {
        var t = new HealthThreshold(PatientId, UserId);
        t.Configure(HealthMetric.BloodGlucose, new MetricLimit(70, false, 180, false), UserId);
        t.Evaluate(HealthMetric.BloodGlucose, 250).Should().BeNull();
    }

    [Fact]
    public void Threshold_rejects_incoherent_configuration()
    {
        var t = new HealthThreshold(PatientId, UserId);
        var invalidRange = () => t.Configure(HealthMetric.HeartRate, new MetricLimit(100, false, 60, false), UserId);
        var noticeWithoutLimit = () => t.Configure(HealthMetric.HeartRate, new MetricLimit(null, true, 100, false), UserId);
        invalidRange.Should().Throw<BusinessRuleException>().Which.Code.Should().Be("INVALID_THRESHOLD_RANGE");
        noticeWithoutLimit.Should().Throw<BusinessRuleException>().Which.Code.Should().Be("THRESHOLD_NOTICE_WITHOUT_LIMIT");
    }

    [Fact]
    public void Permission_presets_and_normalization()
    {
        PermissionSet.FromPreset(AccessProfilePreset.ReadOnly).Should().Be(new PermissionSet(true, false, false, false, false));
        PermissionSet.FromPreset(AccessProfilePreset.Editor).Should().Be(new PermissionSet(true, true, true, true, false));
        // Quem pode editar precisa visualizar.
        new PermissionSet(false, false, true, false, false).Normalize().CanView.Should().BeTrue();
    }

    [Fact]
    public void Revoked_permission_grants_nothing()
    {
        var link = PatientUserPermission.CreateAdministrator(PatientId, UserId, "filha");
        link.Revoke();
        link.ToPermissionSet().HasAny.Should().BeFalse();
        link.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Event_status_transitions_follow_rules()
    {
        var ev = new PatientEvent(PatientId, UserId, PatientEventType.Appointment, "Consulta", DateTimeOffset.UtcNow.AddDays(1), 30, null, null, null);
        ev.ChangeStatus(PatientEventStatus.Completed, "Tudo bem", UserId);
        ev.Status.Should().Be(PatientEventStatus.Completed);
        var reopen = () => ev.ChangeStatus(PatientEventStatus.Scheduled, null, UserId);
        reopen.Should().Throw<BusinessRuleException>().Which.Code.Should().Be("INVALID_STATUS_TRANSITION");
    }

    [Fact]
    public void Medication_schedule_respects_period_and_weekdays()
    {
        var monday = new DateOnly(2026, 10, 19);
        var med = new Medication(PatientId, UserId, new MedicationData("Dipirona", "500 mg", null, null, AdministrationRoute.Oral, null,
            monday, monday.AddDays(6), "2x ao dia", new[] { new TimeOnly(20, 0), new TimeOnly(8, 0) }, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }));

        med.GetScheduledTimes(monday).Should().Equal(new TimeOnly(8, 0), new TimeOnly(20, 0));
        med.GetScheduledTimes(monday.AddDays(1)).Should().BeEmpty();        // terça
        med.GetScheduledTimes(monday.AddDays(9)).Should().BeEmpty();        // após o fim
    }

    [Fact]
    public void Medication_rejects_end_before_start()
    {
        var start = new DateOnly(2026, 1, 10);
        var act = () => new Medication(PatientId, UserId, new MedicationData("X", "1", null, null, AdministrationRoute.Oral, null,
            start, start.AddDays(-1), "1x", Array.Empty<TimeOnly>(), Array.Empty<DayOfWeek>()));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Measurement_requires_complete_and_coherent_blood_pressure()
    {
        MeasurementData Data(short? s, short? d) => new(DateTimeOffset.UtcNow, s, d, null, null, TemperatureUnit.Celsius, null, null,
            null, GlucoseUnit.MgPerDl, null, null, WeightUnit.Kilogram, null);

        ((Action)(() => new HealthMeasurement(PatientId, UserId, Data(120, null)))).Should().Throw<BusinessRuleException>()
            .Which.Code.Should().Be("BLOOD_PRESSURE_INCOMPLETE");
        ((Action)(() => new HealthMeasurement(PatientId, UserId, Data(80, 120)))).Should().Throw<BusinessRuleException>()
            .Which.Code.Should().Be("BLOOD_PRESSURE_INVALID");
        ((Action)(() => new HealthMeasurement(PatientId, UserId, Data(null, null)))).Should().Throw<BusinessRuleException>()
            .Which.Code.Should().Be("MEASUREMENT_EMPTY");
    }

    [Fact]
    public void Measurement_canonical_values_convert_units()
    {
        var m = new HealthMeasurement(PatientId, UserId, new MeasurementData(DateTimeOffset.UtcNow, null, null, null, 100.4m,
            TemperatureUnit.Fahrenheit, null, null, 10m, GlucoseUnit.MmolPerL, null, null, WeightUnit.Kilogram, null));
        var values = m.GetCanonicalValues().ToDictionary(v => v.Metric, v => v.Value);
        values[HealthMetric.Temperature].Should().Be(38.0m);
        values[HealthMetric.BloodGlucose].Should().Be(180.18m);
    }

    [Fact]
    public void Invitation_can_only_be_accepted_by_invited_email_while_open()
    {
        var now = DateTimeOffset.UtcNow;
        var inv = new PatientInvitation(PatientId, "Ana@Email.com", UserId, CareRole.Caregiver, "cuidadora",
            PermissionSet.FromPreset(AccessProfilePreset.ReadOnly), now.AddDays(7));

        var wrongEmail = () => inv.Accept(Guid.NewGuid(), "outra@email.com", now);
        wrongEmail.Should().Throw<ForbiddenException>();

        inv.Accept(Guid.NewGuid(), "ana@email.com", now);
        inv.Status.Should().Be(InvitationStatus.Accepted);

        var expired = new PatientInvitation(PatientId, "b@b.com", UserId, CareRole.FamilyMember, "filho",
            PermissionSet.FromPreset(AccessProfilePreset.ReadOnly), now.AddDays(-1));
        ((Action)(() => expired.Accept(Guid.NewGuid(), "b@b.com", now))).Should().Throw<BusinessRuleException>()
            .Which.Code.Should().Be("INVITATION_EXPIRED");
    }

    [Fact]
    public void Patient_age_and_erasure()
    {
        var p = new Patient("P000001", UserId, "Maria Silva", new DateOnly(1950, 10, 24), "Curitiba", "80010000",
            Ethnicity.Branca, "52998224725", "hash", Gender.Female, null, null, 160);
        p.AgeOn(new DateOnly(2026, 10, 23)).Should().Be(75);
        p.AgeOn(new DateOnly(2026, 10, 24)).Should().Be(76);

        p.Erase(DateTimeOffset.UtcNow);
        p.Cpf.Should().BeNull();
        p.FullName.Should().NotContain("Maria");
        p.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Soft_delete_keeps_original_author()
    {
        var other = Guid.NewGuid();
        var s = new SymptomRecord(PatientId, UserId, SymptomType.Symptom, "Dor de cabeça", DateTimeOffset.UtcNow);
        s.MarkAsDeleted(other, DateTimeOffset.UtcNow);
        s.MarkAsDeleted(Guid.NewGuid(), DateTimeOffset.UtcNow);
        s.DeletedByUserId.Should().Be(other);
        s.CreatedByUserId.Should().Be(UserId);
    }
}
