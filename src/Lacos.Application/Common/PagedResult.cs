namespace Lacos.Application.Common;

/// <summary>
/// Resultado paginado padrão das listagens. Paginar é obrigatório em listas que crescem com o tempo
/// (histórico, métricas, auditoria) para manter o tempo de resposta abaixo de 2 segundos (RNF09).
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

/// <summary>Parâmetros de paginação com limites de segurança (evita consultas gigantes / abuso).</summary>
public sealed record PageQuery(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;

    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Skip => (SafePage - 1) * SafePageSize;
}
