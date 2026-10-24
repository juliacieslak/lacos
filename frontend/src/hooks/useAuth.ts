// acessa o usuário autenticado e as ações de login/logout (Auth0 + sessão do backend).
import { useAuth0 } from "@auth0/auth0-react";
import { useCallback } from "react";
import { sessionService } from "../services/sessionService";

export function useAuth() {
  const { user, isAuthenticated, isLoading, loginWithRedirect, logout: auth0Logout, error } = useAuth0();

  // logout: revoga o token no backend (efeito imediato) e encerra a sessão no Auth0.
  const logout = useCallback(async () => {
    try {
      await sessionService.logout();
    } catch {
      // mesmo que a API esteja indisponível, a sessão local deve ser encerrada.
    }
    await auth0Logout({ logoutParams: { returnTo: `${window.location.origin}/login` } });
  }, [auth0Logout]);

  const login = useCallback(
    (returnTo = "/dashboard", options?: { email?: string; signup?: boolean }) =>
      loginWithRedirect({
        appState: { returnTo },
        authorizationParams: {
          login_hint: options?.email || undefined,
          screen_hint: options?.signup ? "signup" : undefined,
        },
      }),
    [loginWithRedirect],
  );

  return { user, isAuthenticated, isLoading, error, login, logout };
}
