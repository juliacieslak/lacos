using Lacos.Application.Interfaces;
using Lacos.Domain.Entities;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Resolve o usuário de domínio correspondente à identidade autenticada (Auth0 "sub").
/// O resultado é armazenado em cache durante o escopo da requisição para evitar consultas repetidas.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Retorna o usuário registrado e ativo; lança exceção caso contrário.</summary>
    Task<User> GetRequiredUserAsync(CancellationToken ct = default);
    /// <summary>Atalho para o identificador interno do usuário.</summary>
    Task<Guid> GetRequiredUserIdAsync(CancellationToken ct = default);
}

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private User? _cached;

    public CurrentUserContext(ICurrentUser currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    // Fluxo: valida autenticação → busca por auth0_user_id → valida se está ativo → guarda em cache.
    public async Task<User> GetRequiredUserAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return _cached;
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.Auth0UserId))
            throw new ForbiddenException("Usuário não autenticado.", "UNAUTHENTICATED");

        var user = await _users.GetByAuth0IdAsync(_currentUser.Auth0UserId, ct) ?? throw new UserNotRegisteredException();
        if (!user.IsActive) throw new ForbiddenException("Usuário inativo.", "USER_INACTIVE");
        _cached = user;
        return user;
    }

    public async Task<Guid> GetRequiredUserIdAsync(CancellationToken ct = default) =>
        (await GetRequiredUserAsync(ct)).Id;
}
