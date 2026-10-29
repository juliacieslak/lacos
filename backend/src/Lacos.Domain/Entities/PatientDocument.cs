using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Documento/imagem anexado ao paciente (tabela <c>patient_documents</c>) — RF19 / US03 / UC08.
///
/// Regras (RNF07 e US03):
/// - Formatos aceitos: PDF, JPG e PNG (validados por extensão, content-type e assinatura binária).
/// - Tamanho máximo de 10 MB por arquivo (configurável).
/// - O conteúdo é criptografado com AES-256 antes do envio ao storage (RNF02).
/// - O documento fica vinculado ao paciente e, opcionalmente, a um evento da agenda (ex.: resultado de exame).
/// </summary>
public class PatientDocument : PatientRecord
{
    private PatientDocument() { }

    public PatientDocument(Guid patientId, Guid uploadedByUserId, string fileName, string contentType, long sizeBytes,
        string sha256, DocumentCategory category, string? description, Guid? relatedEventId)
        : base(patientId, uploadedByUserId)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        Category = category;
        Description = description?.Trim();
        RelatedEventId = relatedEventId;
        StoragePath = $"patients/{patientId:N}/{Id:N}";
    }

    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    /// <summary>Hash SHA-256 do conteúdo original, para verificação de integridade.</summary>
    public string Sha256 { get; private set; } = string.Empty;
    /// <summary>Caminho do objeto (criptografado) no storage.</summary>
    public string StoragePath { get; private set; } = string.Empty;
    public DocumentCategory Category { get; private set; }
    [SensitiveData] public string? Description { get; private set; }
    public Guid? RelatedEventId { get; private set; }

    public override string AuditEntityType => "PATIENT_DOCUMENT";

    public void UpdateMetadata(DocumentCategory category, string? description, Guid? relatedEventId, Guid userId)
    {
        Category = category;
        Description = description?.Trim();
        RelatedEventId = relatedEventId;
        TouchBy(userId);
    }
}
