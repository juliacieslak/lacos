using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Entities;
using Lacos.Domain.Interfaces;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lacos.Infrastructure.Persistence;

/// <summary>
/// Exclusão definitiva dos dados clínicos de um paciente (RNF01 - LGPD: "opção de exclusão definitiva de dados").
///
/// Fluxo:
/// 1. Remove do storage todos os arquivos do paciente (inclusive de documentos com exclusão lógica).
/// 2. Remove fisicamente os registros clínicos (ignorando o filtro de exclusão lógica), na ordem das dependências.
/// 3. Remove preferências de notificação e limites clínicos.
/// 4. Redige (anula) os valores antigos/novos da trilha de auditoria do paciente por meio da função
///    <c>redact_patient_audit_values</c>, única operação de alteração permitida pela trigger de imutabilidade.
/// As remoções são adicionadas à unidade de trabalho e confirmadas junto com a anonimização do paciente.
/// </summary>
public sealed class PatientDataEraser : IPatientDataEraser
{
    private readonly LacosDbContext _context;
    private readonly IFileStorage _storage;
    private readonly ILogger<PatientDataEraser> _logger;

    public PatientDataEraser(LacosDbContext context, IFileStorage storage, ILogger<PatientDataEraser> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    public async Task EraseClinicalDataAsync(Guid patientId, CancellationToken ct = default)
    {
        var documents = await _context.PatientDocuments.IgnoreQueryFilters().Where(d => d.PatientId == patientId).ToListAsync(ct);
        if (documents.Count > 0) await _storage.DeleteAsync(documents.Select(d => d.StoragePath), ct);

        await RemoveAllAsync(_context.PatientDocuments, patientId, ct);
        await RemoveAllAsync(_context.MedicationAdministrations, patientId, ct);
        await RemoveAllAsync(_context.Medications, patientId, ct);
        await RemoveAllAsync(_context.CareRoutineCompletions, patientId, ct);
        await RemoveAllAsync(_context.CareRoutineTasks, patientId, ct);
        await RemoveAllAsync(_context.HealthMeasurements, patientId, ct);
        await RemoveAllAsync(_context.SymptomRecords, patientId, ct);
        await RemoveAllAsync(_context.CareDiaryEntries, patientId, ct);
        await RemoveAllAsync(_context.HealthConditions, patientId, ct);
        await RemoveAllAsync(_context.Surgeries, patientId, ct);
        await RemoveAllAsync(_context.Allergies, patientId, ct);
        await RemoveAllAsync(_context.PatientEvents, patientId, ct);

        _context.HealthThresholds.RemoveRange(await _context.HealthThresholds.Where(t => t.PatientId == patientId).ToListAsync(ct));
        _context.NotificationPreferences.RemoveRange(await _context.NotificationPreferences.Where(p => p.PatientId == patientId).ToListAsync(ct));
        _context.Notifications.RemoveRange(await _context.Notifications.Where(n => n.PatientId == patientId).ToListAsync(ct));

        if (_context.Database.IsNpgsql())
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT redact_patient_audit_values({patientId})", ct);

        _logger.LogWarning("Dados clínicos do paciente {PatientId} removidos definitivamente (LGPD)", patientId);
    }

    // As remoções físicas não geram novos registros de auditoria com valores (o interceptador ignora entidades removidas
    // com a marcação abaixo), pois isso recriaria na trilha os dados que a LGPD manda apagar.
    private async Task RemoveAllAsync<T>(DbSet<T> set, Guid patientId, CancellationToken ct) where T : Domain.Common.PatientRecord
    {
        var items = await set.IgnoreQueryFilters().Where(e => e.PatientId == patientId).ToListAsync(ct);
        set.RemoveRange(items);
        ErasureScope.Mark(_context, items);
    }
}

/// <summary>Marca entidades removidas pelo fluxo de exclusão definitiva para que a auditoria não registre seus valores.</summary>
internal static class ErasureScope
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DbContext, HashSet<object>> Erased = new();

    public static void Mark(DbContext context, IEnumerable<object> entities)
    {
        var set = Erased.GetOrCreateValue(context);
        foreach (var e in entities) set.Add(e);
    }

    public static bool IsErased(DbContext context, object entity) =>
        Erased.TryGetValue(context, out var set) && set.Contains(entity);
}
