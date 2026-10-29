using System.Data.Common;
using Lacos.Domain.Exceptions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lacos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Permite que a API seja inicializada antes da configuração definitiva do Supabase.
/// Quando a connection string não foi informada, a tentativa de abrir conexão é interrompida com uma
/// <see cref="ExternalServiceException"/> (HTTP 503) e mensagem clara, em vez de um erro genérico do driver.
/// </summary>
public sealed class DatabaseConfigurationGuardInterceptor : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        EnsureConfigured(connection);
        return base.ConnectionOpening(connection, eventData, result);
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        EnsureConfigured(connection);
        return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }

    private static void EnsureConfigured(DbConnection connection)
    {
        if (string.IsNullOrWhiteSpace(connection.ConnectionString))
            throw new ExternalServiceException("Banco de dados",
                "conexão com o Supabase não configurada (defina ConnectionStrings__Supabase).", "DATABASE_NOT_CONFIGURED");
    }
}
