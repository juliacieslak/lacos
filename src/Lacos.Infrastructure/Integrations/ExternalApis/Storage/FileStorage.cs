using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Integrations:Storage";
    /// <summary>"Supabase" (produção) ou "Local" (desenvolvimento).</summary>
    public string Provider { get; set; } = "Local";
    public string SupabaseUrl { get; set; } = string.Empty;
    /// <summary>Chave service_role do Supabase (somente no servidor; nunca expor ao front-end).</summary>
    public string ServiceRoleKey { get; set; } = string.Empty;
    /// <summary>Bucket PRIVADO onde os anexos (já criptografados) são gravados.</summary>
    public string Bucket { get; set; } = "patient-documents";
    public string LocalPath { get; set; } = "storage";
}

/// <summary>
/// Armazenamento no Supabase Storage via API REST (bucket privado).
/// Os arquivos chegam aqui já criptografados com AES-256 (RNF02), portanto nem o provedor de storage
/// tem acesso ao conteúdo em claro.
/// </summary>
public sealed class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _http;
    private readonly StorageOptions _options;

    public SupabaseFileStorage(HttpClient http, IOptions<StorageOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.SupabaseUrl.TrimEnd('/') + "/storage/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        _http.DefaultRequestHeaders.Add("apikey", _options.ServiceRoleKey);
    }

    public async Task UploadAsync(string path, byte[] content, string contentType, CancellationToken ct = default)
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"object/{_options.Bucket}/{path}") { Content = body };
        request.Headers.Add("x-upsert", "false");
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("Supabase Storage", $"falha no upload ({(int)response.StatusCode}).");
    }

    public async Task<byte[]> DownloadAsync(string path, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"object/{_options.Bucket}/{path}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) throw new NotFoundException("Arquivo", path);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("Supabase Storage", $"falha no download ({(int)response.StatusCode}).");
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task DeleteAsync(IEnumerable<string> paths, CancellationToken ct = default)
    {
        var prefixes = paths.ToList();
        if (prefixes.Count == 0) return;
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"object/{_options.Bucket}")
        {
            Content = JsonContent.Create(new { prefixes })
        };
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("Supabase Storage", $"falha ao remover arquivos ({(int)response.StatusCode}).");
    }
}

/// <summary>Armazenamento em disco local — apenas para desenvolvimento e testes.</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.LocalPath);
        Directory.CreateDirectory(_root);
    }

    public async Task UploadAsync(string path, byte[] content, string contentType, CancellationToken ct = default)
    {
        var full = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content, ct);
    }

    public async Task<byte[]> DownloadAsync(string path, CancellationToken ct = default)
    {
        var full = Resolve(path);
        if (!File.Exists(full)) throw new NotFoundException("Arquivo", path);
        return await File.ReadAllBytesAsync(full, ct);
    }

    public Task DeleteAsync(IEnumerable<string> paths, CancellationToken ct = default)
    {
        foreach (var path in paths)
        {
            var full = Resolve(path);
            if (File.Exists(full)) File.Delete(full);
        }
        return Task.CompletedTask;
    }

    // Impede path traversal: o caminho final precisa estar dentro da pasta raiz.
    private string Resolve(string path)
    {
        var full = Path.GetFullPath(Path.Combine(_root, path));
        if (!full.StartsWith(_root, StringComparison.Ordinal)) throw new UnauthorizedAccessException("Caminho inválido.");
        return full;
    }
}
