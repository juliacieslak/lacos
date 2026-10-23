using Lacos.Domain.Exceptions;

namespace Lacos.Application.Validators;

/// <summary>
/// Validação de arquivos anexados (RNF07: "aceitando apenas formatos seguros (PDF, JPG, PNG)").
///
/// Algoritmo em três camadas (defesa em profundidade):
/// 1. Extensão do nome do arquivo deve ser .pdf, .jpg, .jpeg ou .png.
/// 2. Content-Type declarado deve ser compatível com a extensão.
/// 3. Assinatura binária ("magic number") dos primeiros bytes deve corresponder ao formato:
///    PDF = 25 50 44 46 ("%PDF"); PNG = 89 50 4E 47 0D 0A 1A 0A; JPEG = FF D8 FF.
/// Retorna o content-type canônico a ser armazenado.
/// </summary>
public static class FileSignatureValidator
{
    private static readonly Dictionary<string, (string ContentType, byte[] Signature)> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }),
        [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF })
    };

    public static string Validate(string fileName, string declaredContentType, ReadOnlySpan<byte> content)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (!Allowed.TryGetValue(extension, out var rule))
            throw new ValidationFailedException("file", "Formato não permitido. Envie arquivos PDF, JPG ou PNG.");

        var declared = (declaredContentType ?? string.Empty).Split(';')[0].Trim();
        var compatible = declared.Equals(rule.ContentType, StringComparison.OrdinalIgnoreCase)
                         || (rule.ContentType == "image/jpeg" && declared.Equals("image/jpg", StringComparison.OrdinalIgnoreCase))
                         || declared.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);
        if (!compatible)
            throw new ValidationFailedException("file", "O tipo do arquivo não corresponde à extensão informada.");

        if (content.Length < rule.Signature.Length || !content[..rule.Signature.Length].SequenceEqual(rule.Signature))
            throw new ValidationFailedException("file", "O conteúdo do arquivo não corresponde a um PDF, JPG ou PNG válido.");

        return rule.ContentType;
    }

    /// <summary>
    /// Remove caminhos e caracteres perigosos do nome (evita path traversal e injeção em cabeçalhos HTTP),
    /// limitando a 200 caracteres e preservando a extensão.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName ?? "arquivo");
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '"', '\'', ';', '\r', '\n' }).ToHashSet();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(cleaned))) cleaned = "arquivo" + Path.GetExtension(cleaned);
        if (cleaned.Length > 200)
        {
            var ext = Path.GetExtension(cleaned);
            cleaned = cleaned[..(200 - ext.Length)] + ext;
        }
        return cleaned;
    }
}
