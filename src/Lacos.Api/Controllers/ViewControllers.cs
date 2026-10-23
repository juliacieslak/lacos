using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Services;
using Lacos.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lacos.Api.Controllers;

/// <summary>Tela principal / Dashboard (WF01).</summary>
[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;
    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get([FromQuery] Guid? patientId, CancellationToken ct) =>
        Ok(await _dashboard.GetAsync(patientId, ct));
}

/// <summary>Agenda de cuidados consolidada (WF03): visão Dia, Semana ou Mês.</summary>
[Route("api/agenda")]
public sealed class AgendaController : ApiControllerBase
{
    private readonly IAgendaService _agenda;
    public AgendaController(IAgendaService agenda) => _agenda = agenda;

    [HttpGet]
    public async Task<ActionResult<AgendaResponse>> Get([FromQuery] Guid? patientId, [FromQuery] DateOnly? date,
        [FromQuery] AgendaView view = AgendaView.Day, CancellationToken ct = default) =>
        Ok(await _agenda.GetAsync(patientId, date, view, ct));
}

/// <summary>Linha do tempo (US04) e histórico de alterações (RF06) do paciente.</summary>
[Route("api/patients/{patientId:guid}")]
public sealed class PatientHistoryController : ApiControllerBase
{
    private readonly ITimelineService _timeline;
    private readonly IAuditLogQueryService _audit;

    public PatientHistoryController(ITimelineService timeline, IAuditLogQueryService audit)
    {
        _timeline = timeline;
        _audit = audit;
    }

    /// <summary>Linha do tempo em ordem decrescente, filtrável por categorias (ex.: ?categories=MEDICATION&amp;categories=SYMPTOM).</summary>
    [HttpGet("timeline")]
    public async Task<ActionResult<PagedResult<TimelineItemResponse>>> Timeline(Guid patientId, [FromQuery] TimelineCategory[]? categories,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _timeline.GetAsync(patientId, categories, from, to, new PageQuery(page, pageSize), ct));

    /// <summary>Histórico de inclusões, alterações e exclusões com o responsável por cada ação (RF06/RF10).</summary>
    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> History(Guid patientId, [FromQuery] string? entityType,
        [FromQuery] Guid? entityId, [FromQuery] AuditAction? action, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeViews = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _audit.ListAsync(patientId, entityType, entityId, action, from, to, includeViews, new PageQuery(page, pageSize), ct));
}

/// <summary>Documentos anexados (RF19 / US03).</summary>
[Route("api/patients/{patientId:guid}/documents")]
public sealed class DocumentsController : ApiControllerBase
{
    private readonly IPatientDocumentService _documents;
    public DocumentsController(IPatientDocumentService documents) => _documents = documents;

    /// <summary>Upload multipart (campo "file"). Limite de 10 MB; formatos PDF, JPG e PNG (RNF07).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("uploads")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DocumentResponse>> Upload(Guid patientId, IFormFile file, [FromForm] DocumentCategory category,
        [FromForm] string? description, [FromForm] Guid? relatedEventId, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await _documents.UploadAsync(patientId,
            new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length, category, description, relatedEventId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentResponse>>> List(Guid patientId, [FromQuery] DocumentCategory? category,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _documents.ListAsync(patientId, category, new PageQuery(page, pageSize), ct));

    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<DocumentResponse>> Get(Guid patientId, Guid documentId, CancellationToken ct) =>
        Ok(await _documents.GetAsync(patientId, documentId, ct));

    /// <summary>
    /// Conteúdo do arquivo para visualização na tela (inline) ou download (?download=true).
    /// </summary>
    [HttpGet("{documentId:guid}/content")]
    [Produces("application/pdf", "image/png", "image/jpeg")]
    public async Task<IActionResult> Content(Guid patientId, Guid documentId, [FromQuery] bool download, CancellationToken ct)
    {
        var content = await _documents.DownloadAsync(patientId, documentId, ct);
        if (download) return File(content.Content, content.ContentType, content.FileName);
        Response.Headers.ContentDisposition = $"inline; filename=\"{content.FileName}\"";
        return File(content.Content, content.ContentType);
    }

    [HttpPut("{documentId:guid}")]
    public async Task<ActionResult<DocumentResponse>> Update(Guid patientId, Guid documentId, UpdateDocumentRequest request, CancellationToken ct) =>
        Ok(await _documents.UpdateAsync(patientId, documentId, request, ct));

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid patientId, Guid documentId, CancellationToken ct)
    {
        await _documents.DeleteAsync(patientId, documentId, ct);
        return NoContent();
    }
}

/// <summary>Notificações do usuário e preferências por paciente (RF12 / UC10).</summary>
[Route("api/notifications")]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notifications;
    private readonly INotificationPreferenceService _preferences;

    public NotificationsController(INotificationService notifications, INotificationPreferenceService preferences)
    {
        _notifications = notifications;
        _preferences = preferences;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationListResponse>> List([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _notifications.ListMineAsync(unreadOnly, new PageQuery(page, pageSize), ct));

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(notificationId, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<object>> MarkAllRead(CancellationToken ct) => Ok(new { updated = await _notifications.MarkAllReadAsync(ct) });

    [HttpGet("preferences")]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceResponse>>> Preferences(CancellationToken ct) =>
        Ok(await _preferences.ListMineAsync(ct));

    [HttpPut("preferences/{patientId:guid}")]
    public async Task<ActionResult<NotificationPreferenceResponse>> UpdatePreferences(Guid patientId, NotificationPreferenceRequest request,
        CancellationToken ct) => Ok(await _preferences.UpdateAsync(patientId, request, ct));
}

/// <summary>Consultas auxiliares a serviços externos (CEP e CID).</summary>
[Route("api/lookups")]
[Lacos.Api.Security.AllowIncompleteProfile]
public sealed class LookupsController : ApiControllerBase
{
    private readonly ILookupService _lookup;
    public LookupsController(ILookupService lookup) => _lookup = lookup;

    [HttpGet("cep/{cep}")]
    public async Task<ActionResult<Lacos.Application.Interfaces.Integrations.AddressLookupResult>> Cep(string cep, CancellationToken ct) =>
        Ok(await _lookup.LookupCepAsync(cep, ct));

    [HttpGet("cid")]
    public async Task<ActionResult<IReadOnlyList<Lacos.Application.Interfaces.Integrations.CidLookupResult>>> Cid([FromQuery] string term,
        CancellationToken ct) => Ok(await _lookup.SearchCidAsync(term, ct));
}
