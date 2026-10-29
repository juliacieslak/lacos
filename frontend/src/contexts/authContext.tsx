// provider do Auth0 integrado ao React Router e ao cliente HTTP.
import { Auth0Provider, useAuth0, type AppState } from "@auth0/auth0-react";
import { useEffect, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { env, missingAuthConfig } from "../config/env";
import { setSessionEndedHandler, setTokenProvider } from "../services/api";
import ConfigurationErrorPage from "../pages/configurationErrorPage";

interface Auth0ProviderWithNavigateProps {
  children: ReactNode;
}

// o Auth0 React SDK usa Authorization Code Flow com PKCE (fluxo recomendado para SPAs); nenhum client secret é usado.
export default function Auth0ProviderWithNavigate({ children }: Auth0ProviderWithNavigateProps) {
  // permite mudar de página pelo React Router.
  const navigate = useNavigate();

  // sem configuração do Auth0 a aplicação inicia, mas mostra o que falta configurar (em vez de tela em branco).
  if (missingAuthConfig.length > 0) return <ConfigurationErrorPage missing={missingAuthConfig} />;

  // executado quando o Auth0 termina de processar o login: volta para a página solicitada antes do login.
  const onRedirectCallback = (appState?: AppState) => {
    navigate(appState?.returnTo || "/dashboard", { replace: true });
  };

  return (
    <Auth0Provider
      domain={env.auth0Domain}
      clientId={env.auth0ClientId}
      authorizationParams={{
        // o Auth0 retorna para a raiz da aplicação após o login.
        redirect_uri: window.location.origin,
        // API protegida: o access token emitido terá esta audiência, validada pelo backend.
        audience: env.auth0Audience,
        scope: "openid profile email offline_access",
      }}
      onRedirectCallback={onRedirectCallback}
      // refresh tokens com rotação (configurar no painel do Auth0) mantêm a sessão após recarregar a página
      // sem depender de cookies de terceiros. O cache em localStorage é necessário para isso em navegadores atuais.
      useRefreshTokens
      useRefreshTokensFallback
      cacheLocation="localstorage"
    >
      <ApiAuthBridge />
      {children}
    </Auth0Provider>
  );
}

// conecta o Auth0 ao cliente HTTP: fornece o access token e trata sessão encerrada pelo backend.
function ApiAuthBridge() {
  const { getAccessTokenSilently, isAuthenticated, logout } = useAuth0();

  useEffect(() => {
    setTokenProvider(isAuthenticated ? () => getAccessTokenSilently() : null);
    return () => setTokenProvider(null);
  }, [getAccessTokenSilently, isAuthenticated]);

  useEffect(() => {
    // RNF03: o backend recusa tokens após 15 min de inatividade → encerra a sessão local também.
    setSessionEndedHandler((code) => {
      logout({ logoutParams: { returnTo: `${window.location.origin}/login?motivo=${code === "SESSION_EXPIRED" ? "inatividade" : "encerrada"}` } });
    });
    return () => setSessionEndedHandler(null);
  }, [logout]);

  return null;
}
