using Lacos.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lacos.Infrastructure.Persistence.Context;

/// <summary>
/// Fábrica usada apenas em tempo de design pelas ferramentas do EF Core (dotnet ef migrations ...).
/// Usa uma chave fictícia: as migrations só precisam do modelo, nunca leem nem gravam dados.
/// A connection string pode ser informada pela variável de ambiente LACOS_MIGRATIONS_CONNECTION.
/// </summary>
public sealed class LacosDbContextFactory : IDesignTimeDbContextFactory<LacosDbContext>
{
    public LacosDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("LACOS_MIGRATIONS_CONNECTION")
                         ?? "Host=localhost;Database=lacos;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<LacosDbContext>()
            .UseNpgsql(connection, o => o.MigrationsAssembly(typeof(LacosDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;
        var encryption = new AesGcmEncryptionService(Microsoft.Extensions.Options.Options.Create(new EncryptionOptions
        {
            DataKey = Convert.ToBase64String(new byte[32]),
            BlindIndexKey = Convert.ToBase64String(new byte[32])
        }));
        return new LacosDbContext(options, encryption);
    }
}
