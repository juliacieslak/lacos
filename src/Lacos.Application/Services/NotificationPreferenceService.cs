using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Domain.Entities;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>Configuração das categorias de notificação por paciente (RF12 / UC10 "Configurar Notificações").</summary>
public interface INotificationPreferenceService
{
    Task<IReadOnlyList<NotificationPreferenceResponse>> ListMineAsync(CancellationToken ct = default);
    Task<NotificationPreferenceResponse> UpdateAsync(Guid patientId, NotificationPreferenceRequest request, CancellationToken ct = default);
}

public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly ICurrentUserContext _userContext;
    private readonly IPatientAccessService _access;
    private readonly IPatientPermissionRepository _permissions;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationPreferenceService(ICurrentUserContext userContext, IPatientAccessService access,
        IPatientPermissionRepository permissions, INotificationPreferenceRepository preferences, IUnitOfWork unitOfWork)
    {
        _userContext = userContext;
        _access = access;
        _permissions = permissions;
        _preferences = preferences;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Lista as preferências de todos os pacientes que o usuário acompanha (cria as que faltarem com o padrão).</summary>
    public async Task<IReadOnlyList<NotificationPreferenceResponse>> ListMineAsync(CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        var links = await _permissions.ListActiveByUserAsync(userId, ct);
        var prefs = (await _preferences.ListByUserAsync(userId, ct)).ToDictionary(p => p.PatientId);
        var result = new List<NotificationPreferenceResponse>();
        var created = false;
        foreach (var link in links.Where(l => l.CanViewPatient))
        {
            if (!prefs.TryGetValue(link.PatientId, out var pref))
            {
                pref = new NotificationPreference(link.PatientId, userId);
                await _preferences.AddAsync(pref, ct);
                created = true;
            }
            result.Add(Map(pref, link.Patient?.FullName ?? string.Empty));
        }
        if (created) await _unitOfWork.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>
    /// Atualiza as preferências do usuário para um paciente. Regra: só é possível configurar notificações
    /// de pacientes que o usuário pode consultar (as notificações expõem dados do paciente).
    /// </summary>
    public async Task<NotificationPreferenceResponse> UpdateAsync(Guid patientId, NotificationPreferenceRequest r, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.View, ct);
        var pref = await _preferences.GetAsync(patientId, access.UserId, ct);
        if (pref is null)
        {
            pref = new NotificationPreference(patientId, access.UserId);
            await _preferences.AddAsync(pref, ct);
        }
        pref.Update(r.MedicationReminder, r.MedicationMissed, r.AppointmentReminder, r.CareRoutineCheckin,
            r.PatientDataAdded, r.HealthThresholdAlert, r.IsActive);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(pref, access.Patient.FullName);
    }

    private static NotificationPreferenceResponse Map(NotificationPreference p, string patientName) =>
        new(p.PatientId, patientName, p.MedicationReminder, p.MedicationMissed, p.AppointmentReminder,
            p.CareRoutineCheckin, p.PatientDataAdded, p.HealthThresholdAlert, p.IsActive);
}
