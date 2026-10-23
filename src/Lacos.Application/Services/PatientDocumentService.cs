using System.Security.Cryptography;
using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Application.Mappings;
using Lacos.Application.Options;
using Lacos.Application.Validators;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Lacos.Application.Services;

/// <summary>
/// Anexos de imagens e documentos do paciente (RF19 / US03 / UC08), com as restrições de RNF07 e a criptografia de RNF02.
/// </summary>
public interface IPatientDocumentService
{
    Task<DocumentResponse> UploadAsync(Guid patientId, UploadDocumentCommand command, CancellationToken ct = default);
    Task<PagedResult<DocumentResponse>> ListAsync(Guid patientId, DocumentCategory? category, PageQuery page, CancellationToken ct = default);
    Task<DocumentResponse> GetAsync(Guid patientId, Guid documentId, CancellationToken ct = default);
    Task<DocumentContent> DownloadAsync(Guid patientId, Guid documentId, CancellationToken ct = default);
    Task<DocumentResponse> UpdateAsync(Guid patientId, Guid documentId, UpdateDocumentRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid patientId, Guid documentId, CancellationToken ct = default);
}

public sealed class PatientDocumentService : IPatientDocumentService
{
    private readonly IPatientRecordRepository<PatientDocument> _documents;
    private readonly IPatientEventRepository _events;
    private readonly IFileStorage _storage;
    private readonly IEncryptionService _encryption;
    private readonly PatientRecordDependencies _deps;
    private readonly DocumentOptions _options;

    public PatientDocumentService(IPatientRecordRepository<PatientDocument> documents, IPatientEventRepository events, IFileStorage storage,
        IEncryptionService encryption, PatientRecordDependencies deps, IOptions<DocumentOptions> options)
    {
        _documents = documents;
        _events = events;
        _storage = storage;
        _encryption = encryption;
        _deps = deps;
        _options = options.Value;
    }

    /// <summary>
    /// Upload (US03). Fluxo:
    /// 1. Autoriza (permissão de adicionar).
    /// 2. Valida tamanho (≤ 10 MB) antes e depois da leitura (o Content-Length pode ser falsificado).
    /// 3. Valida formato por extensão, content-type e assinatura binária ("magic number") — impede, por exemplo,
    ///    um executável renomeado para .pdf.
    /// 4. Calcula SHA-256 do original (integridade), criptografa com AES-256 e envia ao storage.
    /// 5. Grava os metadados; se a gravação falhar, remove o arquivo enviado (compensação) para não deixar órfãos.
    /// 6. Notifica os membros (nova informação adicionada).
    /// </summary>
    public async Task<DocumentResponse> UploadAsync(Guid patientId, UploadDocumentCommand c, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.Add, ct);
        if (c.Length <= 0) throw new ValidationFailedException("file", "O arquivo está vazio.");
        if (c.Length > _options.MaxFileSizeBytes)
            throw new ValidationFailedException("file", $"O arquivo excede o limite de {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
        if (c.RelatedEventId is { } eventId && await _events.GetForPatientAsync(patientId, eventId, ct) is null)
            throw new NotFoundException("Compromisso", eventId);

        var bytes = await ReadLimitedAsync(c.Content, _options.MaxFileSizeBytes, ct);
        var contentType = FileSignatureValidator.Validate(c.FileName, c.ContentType, bytes);
        var safeName = FileSignatureValidator.SanitizeFileName(c.FileName);
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var document = new PatientDocument(patientId, access.UserId, safeName, contentType, bytes.LongLength, sha256,
            c.Category, c.Description, c.RelatedEventId);

        await _storage.UploadAsync(document.StoragePath, _encryption.EncryptBytes(bytes), "application/octet-stream", ct);
        try
        {
            await _documents.AddAsync(document, ct);
            await _deps.Notifications.EnqueueForPatientAsync(patientId, NotificationCategory.PatientDataAdded,
                "Nova informação: Documento", $"Um novo documento foi anexado ao perfil de {access.Patient.FullName}.",
                $"data-added:{document.Id:N}", document.Id, access.UserId, ct);
            await _deps.UnitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await _storage.DeleteAsync(new[] { document.StoragePath }, CancellationToken.None);
            throw;
        }
        return await MapAsync(document, ct);
    }

    public async Task<PagedResult<DocumentResponse>> ListAsync(Guid patientId, DocumentCategory? category, PageQuery page, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var (items, total) = await _documents.ListByPatientAsync(patientId, null, null, 0, int.MaxValue, ct);
        var filtered = category is null ? items : items.Where(d => d.Category == category).ToList();
        var pageItems = filtered.Skip(page.Skip).Take(page.SafePageSize).ToList();
        var names = await _deps.Users.GetNamesAsync(pageItems.SelectMany(d => new[] { d.CreatedByUserId, d.UpdatedByUserId ?? Guid.Empty }), ct);
        await _deps.Audit.RecordViewAsync(access.UserId, patientId, "PATIENT_DOCUMENT_LIST", null, ct);
        return new PagedResult<DocumentResponse>(pageItems.Select(d => Map(d, names)).ToList(), page.SafePage, page.SafePageSize,
            category is null ? total : filtered.Count);
    }

    public async Task<DocumentResponse> GetAsync(Guid patientId, Guid documentId, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var doc = await FindAsync(patientId, documentId, ct);
        await _deps.Audit.RecordViewAsync(access.UserId, patientId, doc.AuditEntityType, doc.Id, ct);
        return await MapAsync(doc, ct);
    }

    /// <summary>
    /// Download/visualização (US03 "visualização prévia", US04 "abrir e visualizar os documentos diretamente na tela").
    /// Descriptografa o conteúdo e confere o SHA-256 para garantir que o arquivo não foi corrompido/adulterado.
    /// </summary>
    public async Task<DocumentContent> DownloadAsync(Guid patientId, Guid documentId, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var doc = await FindAsync(patientId, documentId, ct);
        var plain = _encryption.DecryptBytes(await _storage.DownloadAsync(doc.StoragePath, ct));
        var hash = Convert.ToHexString(SHA256.HashData(plain)).ToLowerInvariant();
        if (hash != doc.Sha256)
            throw new BusinessRuleException("DOCUMENT_INTEGRITY_FAILURE", "O arquivo armazenado não confere com o original.");
        await _deps.Audit.RecordViewAsync(access.UserId, patientId, "PATIENT_DOCUMENT_CONTENT", doc.Id, ct);
        return new DocumentContent(plain, doc.FileName, doc.ContentType);
    }

    public async Task<DocumentResponse> UpdateAsync(Guid patientId, Guid documentId, UpdateDocumentRequest r, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.Edit, ct);
        var doc = await FindAsync(patientId, documentId, ct);
        if (r.RelatedEventId is { } eventId && await _events.GetForPatientAsync(patientId, eventId, ct) is null)
            throw new NotFoundException("Compromisso", eventId);
        doc.UpdateMetadata(r.Category, r.Description, r.RelatedEventId, access.UserId);
        await _deps.UnitOfWork.SaveChangesAsync(ct);
        return await MapAsync(doc, ct);
    }

    /// <summary>
    /// Exclusão lógica do documento: o arquivo permanece no storage (criptografado) para preservar o histórico (RF06);
    /// a remoção física ocorre apenas na exclusão definitiva do paciente (LGPD).
    /// </summary>
    public async Task DeleteAsync(Guid patientId, Guid documentId, CancellationToken ct = default)
    {
        var access = await _deps.Access.RequireAsync(patientId, PatientPermission.Delete, ct);
        var doc = await FindAsync(patientId, documentId, ct);
        doc.MarkAsDeleted(access.UserId, _deps.Clock.UtcNow);
        await _deps.UnitOfWork.SaveChangesAsync(ct);
    }

    private async Task<PatientDocument> FindAsync(Guid patientId, Guid id, CancellationToken ct) =>
        await _documents.GetForPatientAsync(patientId, id, ct) ?? throw new NotFoundException("Documento", id);

    private async Task<DocumentResponse> MapAsync(PatientDocument d, CancellationToken ct)
    {
        var names = await _deps.Users.GetNamesAsync(new[] { d.CreatedByUserId, d.UpdatedByUserId ?? Guid.Empty }, ct);
        return Map(d, names);
    }

    private static DocumentResponse Map(PatientDocument d, IReadOnlyDictionary<Guid, string> names) =>
        new(d.Id, d.PatientId, d.FileName, d.ContentType, d.SizeBytes, d.Category, d.Description, d.RelatedEventId, d.ToRecordInfo(names));

    // Lê no máximo (limite + 1) bytes: se ultrapassar, o arquivo é rejeitado sem consumir memória ilimitada.
    private static async Task<byte[]> ReadLimitedAsync(Stream stream, long limit, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > limit)
                throw new ValidationFailedException("file", $"O arquivo excede o limite de {limit / (1024 * 1024)} MB.");
        }
        return buffer.ToArray();
    }
}
