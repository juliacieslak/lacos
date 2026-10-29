using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Common;

/// <summary>
/// Cache + coalescência de requisições ("single flight") para APIs externas públicas:
/// - Resultados recentes são reaproveitados do cache (reduz latência e respeita limites de uso dos provedores).
/// - Requisições idênticas simultâneas compartilham a MESMA chamada externa, evitando requisições duplicadas
///   (ex.: duplo clique ou vários usuários pesquisando o mesmo CEP ao mesmo tempo).
/// Falhas não são armazenadas em cache.
/// </summary>
public sealed class CachedRequestCoalescer
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> _inFlight = new();

    public CachedRequestCoalescer(IMemoryCache cache) => _cache = cache;

    public async Task<T?> GetOrAddAsync<T>(string key, TimeSpan ttl, Func<Task<T?>> factory)
    {
        if (_cache.TryGetValue(key, out T? cached)) return cached;

        var lazy = _inFlight.GetOrAdd(key, _ => new Lazy<Task<object?>>(async () => await factory()));
        try
        {
            var value = (T?)await lazy.Value;
            _cache.Set(key, value, ttl);
            return value;
        }
        finally
        {
            _inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<object?>>>(key, lazy));
        }
    }
}
