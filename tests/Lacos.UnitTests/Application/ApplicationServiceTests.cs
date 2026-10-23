using FluentAssertions;
using Lacos.Application.Common;
using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Application.Services;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;
using Moq;

namespace Lacos.UnitTests.Application;

public class PatientAccessServiceTests
{
    private readonly Mock<ICurrentUserContext> _userContext = new();
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IPatientPermissionRepository> _permissions = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Patient _patient = new("P000001", Guid.NewGuid(), "Maria", new DateOnly(1950, 1, 1), "Curitiba", "80010000",
        Ethnicity.Branca, "52998224725", "h", Gender.Female, null, null, 160);

    private PatientAccessService Sut()
    {
        _userContext.Setup(u => u.GetRequiredUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_userId);
        _patients.Setup(p => p.GetByIdAsync(_patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_patient);
        return new PatientAccessService(_userContext.Object, _patients.Object, _permissions.Object);
    }

    [Fact]
    public async Task Without_link_returns_not_found_to_avoid_enumeration()
    {
        var act = () => Sut().RequireAsync(_patient.Id, PatientPermission.View);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Read_only_member_cannot_add_data()
    {
        _permissions.Setup(p => p.GetAsync(_patient.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientUserPermission(_patient.Id, _userId, CareRole.FamilyMember, "filho", PermissionSet.FromPreset(AccessProfilePreset.ReadOnly)));
        var sut = Sut();

        (await sut.RequireAsync(_patient.Id, PatientPermission.View)).Patient.Should().Be(_patient);
        var act = () => sut.RequireAsync(_patient.Id, PatientPermission.Add);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Revoked_link_loses_access_immediately()
    {
        var link = PatientUserPermission.CreateAdministrator(_patient.Id, _userId, "filha");
        link.Revoke();
        _permissions.Setup(p => p.GetAsync(_patient.Id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var act = () => Sut().RequireAsync(_patient.Id, PatientPermission.View);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Erased_patient_is_not_accessible()
    {
        _permissions.Setup(p => p.GetAsync(_patient.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PatientUserPermission.CreateAdministrator(_patient.Id, _userId, "filha"));
        _patient.Erase(DateTimeOffset.UtcNow);
        var act = () => Sut().RequireAsync(_patient.Id, PatientPermission.View);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class NotificationServiceTests
{
    [Fact]
    public async Task Enqueue_skips_author_and_existing_keys_and_signals_dispatcher()
    {
        var patientId = Guid.NewGuid();
        var author = Guid.NewGuid();
        var alreadyNotified = Guid.NewGuid();
        var recipient = Guid.NewGuid();

        var prefs = new Mock<INotificationPreferenceRepository>();
        prefs.Setup(p => p.ListRecipientsAsync(patientId, NotificationCategory.PatientDataAdded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { author, alreadyNotified, recipient });
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.ExistsByDeduplicationKeyAsync(It.Is<string>(k => k.EndsWith(alreadyNotified.ToString("N"))), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var added = new List<Notification>();
        repo.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => added.Add(n)).Returns(Task.CompletedTask);
        var signal = new Mock<INotificationDispatchSignal>();

        var sut = new NotificationService(repo.Object, prefs.Object, Mock.Of<ICurrentUserContext>(), Mock.Of<IUnitOfWork>(),
            Mock.Of<IDateTimeProvider>(), signal.Object);

        var count = await sut.EnqueueForPatientAsync(patientId, NotificationCategory.PatientDataAdded, "t", "m", "data-added:x", null, author);

        count.Should().Be(1);
        added.Single().UserId.Should().Be(recipient);
        signal.Verify(s => s.Signal(), Times.Once);
    }
}
