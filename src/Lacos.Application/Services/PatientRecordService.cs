using Lacos.Application.Common;
using Lacos.Application.Interfaces;
using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Dependências comuns aos serviços de registros clínicos (agrupadas para evitar construtores extensos).
/// </summary>
public sealed class PatientRecordDependencies
{
    public PatientRecordDependencies(IPatientAccessService access, IAuditService audit, INotificationService notifications,
        IUserRepository users, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        Access = access;
        Audit = audit;
        Notifications = notifications;
        Users = users;
        UnitOfWork = unitOfWork;
        Clock = clock;
    }

    public IPatientAccessService Access { get; }
    public IAuditService Audit { get; }
    public INotificationService Notifications { get; }
    public IUserRepository Users { get; }
    public IUnitOfWork UnitOfWork { get; }
    public IDateTimeProvider Clock { get; }
}

/// <summary>
/// Serviço base (Template Method) para o CRUD de qualquer registro clínico do paciente.
///
/// Centraliza, de forma única (DRY), as regras transversais que a documentação exige para TODO dado do paciente:
/// - Autorização por ação: consultar / adicionar / alterar / excluir (RF05, RF07).
/// - Identificação automática do responsável e da data/hora (RF10) — feita pela entidade + interceptador.
/// - Histórico de inclusões, alterações e exclusões (RF06) — interceptador de auditoria.
/// - Registro de visualizações com IP (RNF04).
/// - Notificação "nova informação adicionada" aos membros que optaram por recebê-la (RF12).
/// - Exclusão lógica para preservar o histórico.
/// As subclasses implementam apenas o que é específico: criação, atualização e mapeamento.
/// </summary>
public abstract class PatientRecordService<TEntity, TRequest, TResponse> where TEntity : PatientRecord
{
    protected PatientRecordService(IPatientRecordRepository<TEntity> repository, PatientRecordDependencies deps)
    {
        Repository = repository;
        Deps = deps;
    }

    protected IPatientRecordRepository<TEntity> Repository { get; }
    protected PatientRecordDependencies Deps { get; }

    /// <summary>Nome amigável do recurso, usado em mensagens (ex.: "Sintoma").</summary>
    protected abstract string ResourceName { get; }

    /// <summary>Constrói a entidade a partir da requisição (regras específicas do tipo).</summary>
    protected abstract Task<TEntity> CreateEntityAsync(PatientAccess access, TRequest request, CancellationToken ct);

    /// <summary>Aplica a atualização na entidade existente.</summary>
    protected abstract Task ApplyUpdateAsync(PatientAccess access, TEntity entity, TRequest request, CancellationToken ct);

    /// <summary>Converte a entidade em DTO de resposta.</summary>
    protected abstract TResponse Map(TEntity entity, IReadOnlyDictionary<Guid, string> userNames);

    /// <summary>Gancho executado após inclusão/alteração e antes do SaveChanges (ex.: avaliar limites clínicos).</summary>
    protected virtual Task AfterChangeAsync(PatientAccess access, TEntity entity, bool created, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Mapeamento assíncrono (pode ser sobrescrito quando a resposta depende de outros dados).</summary>
    protected virtual async Task<TResponse> MapAsync(TEntity entity, CancellationToken ct)
    {
        var names = await LoadNamesAsync(new[] { entity }, ct);
        return Map(entity, names);
    }

    /// <summary>
    /// Inclusão: exige permissão de ADICIONAR. Fluxo: autoriza → cria → executa ganchos → enfileira notificação
    /// → grava tudo em uma transação (incluindo a auditoria gerada pelo interceptador).
    /// </summary>
    public virtual async Task<TResponse> CreateAsync(Guid patientId, TRequest request, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Add, ct);
        var entity = await CreateEntityAsync(access, request, ct);
        await Repository.AddAsync(entity, ct);
        await AfterChangeAsync(access, entity, created: true, ct);
        await NotifyDataAddedAsync(access, entity, ct);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
        return await MapAsync(entity, ct);
    }

    /// <summary>Consulta individual: exige permissão de CONSULTAR e registra a visualização (RNF04).</summary>
    public virtual async Task<TResponse> GetAsync(Guid patientId, Guid id, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var entity = await FindAsync(patientId, id, ct);
        await Deps.Audit.RecordViewAsync(access.UserId, patientId, entity.AuditEntityType, entity.Id, ct);
        return await MapAsync(entity, ct);
    }

    /// <summary>Listagem paginada em ordem cronológica decrescente (US04), com filtro de período.</summary>
    public virtual async Task<PagedResult<TResponse>> ListAsync(Guid patientId, DateTimeOffset? from, DateTimeOffset? to,
        PageQuery page, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var (items, total) = await Repository.ListByPatientAsync(patientId, from, to, page.Skip, page.SafePageSize, ct);
        await Deps.Audit.RecordViewAsync(access.UserId, patientId, $"{AuditTypeName}_LIST", null, ct);
        var names = await LoadNamesAsync(items, ct);
        var mapped = new List<TResponse>(items.Count);
        foreach (var item in items) mapped.Add(await MapListItemAsync(item, names, ct));
        return new PagedResult<TResponse>(mapped, page.SafePage, page.SafePageSize, total);
    }

    /// <summary>Alteração: exige permissão de ALTERAR. O interceptador grava valores antigos e novos (RF06).</summary>
    public virtual async Task<TResponse> UpdateAsync(Guid patientId, Guid id, TRequest request, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Edit, ct);
        var entity = await FindAsync(patientId, id, ct);
        await ApplyUpdateAsync(access, entity, request, ct);
        await AfterChangeAsync(access, entity, created: false, ct);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
        return await MapAsync(entity, ct);
    }

    /// <summary>Exclusão lógica: exige permissão de EXCLUIR (RF07). Registra quem excluiu e quando (RF10).</summary>
    public virtual async Task DeleteAsync(Guid patientId, Guid id, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Delete, ct);
        var entity = await FindAsync(patientId, id, ct);
        entity.MarkAsDeleted(access.UserId, Deps.Clock.UtcNow);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Mapeamento de item de lista (por padrão igual ao individual, sem consultas extras).</summary>
    protected virtual Task<TResponse> MapListItemAsync(TEntity entity, IReadOnlyDictionary<Guid, string> names, CancellationToken ct) =>
        Task.FromResult(Map(entity, names));

    /// <summary>Busca o registro garantindo que pertence ao paciente (proteção contra IDOR).</summary>
    protected async Task<TEntity> FindAsync(Guid patientId, Guid id, CancellationToken ct) =>
        await Repository.GetForPatientAsync(patientId, id, ct) ?? throw new NotFoundException(ResourceName, id);

    /// <summary>Carrega os nomes dos usuários responsáveis (uma única consulta para todos os registros).</summary>
    protected Task<IReadOnlyDictionary<Guid, string>> LoadNamesAsync(IEnumerable<TEntity> entities, CancellationToken ct)
    {
        var ids = entities.SelectMany(e => new[] { e.CreatedByUserId, e.UpdatedByUserId ?? Guid.Empty })
            .Where(id => id != Guid.Empty).Distinct();
        return Deps.Users.GetNamesAsync(ids, ct);
    }

    /// <summary>Data padrão quando o cliente não informa (US02: associa automaticamente data e hora ao salvar).</summary>
    protected DateTimeOffset NowIfNull(DateTimeOffset? value) => value ?? Deps.Clock.UtcNow;

    /// <summary>
    /// Impede datas de ocorrência no futuro (com tolerância de 5 minutos para diferenças de relógio do dispositivo).
    /// </summary>
    protected void EnsureNotInFuture(DateTimeOffset when, string field)
    {
        if (when > Deps.Clock.UtcNow.AddMinutes(5))
            throw new ValidationFailedException(field, "A data/hora não pode estar no futuro.");
    }

    // Converte o nome do tipo (PascalCase) para o padrão UPPER_SNAKE_CASE usado em audit_logs.entity_type.
    private static string AuditTypeName =>
        string.Concat(typeof(TEntity).Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString())).ToUpperInvariant();

    /// <summary>
    /// Notificação "nova informação adicionada" (patient_data_added). A mensagem não contém o conteúdo clínico,
    /// apenas o tipo de registro, para não expor dados sensíveis em canais externos (e-mail/push).
    /// </summary>
    private async Task NotifyDataAddedAsync(PatientAccess access, TEntity entity, CancellationToken ct)
    {
        var names = await Deps.Users.GetNamesAsync(new[] { access.UserId }, ct);
        var author = names.GetValueOrDefault(access.UserId) ?? "Um membro da rede de cuidado";
        await Deps.Notifications.EnqueueForPatientAsync(access.Patient.Id, NotificationCategory.PatientDataAdded,
            $"Nova informação: {ResourceName}",
            $"{author} adicionou um novo registro ({ResourceName.ToLowerInvariant()}) para {access.Patient.FullName}.",
            $"data-added:{entity.Id:N}", entity.Id, access.UserId, ct);
    }
}
