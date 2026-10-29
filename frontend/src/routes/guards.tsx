// proteção de rotas: autenticação (Auth0), sessão no backend, cadastro concluído e consentimento LGPD.
import { useAuth0 } from "@auth0/auth0-react";
import { LogOut, RefreshCw } from "lucide-react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import Button from "../components/ui/button";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Logo from "../components/ui/logo";
import { SessionProvider, useSession } from "../contexts/sessionContext";
import { useAuth } from "../hooks/useAuth";
import { useIdleLogout } from "../hooks/useIdleLogout";

// rotas públicas (login/cadastro): quem já está autenticado vai para o dashboard.
export function PublicOnlyRoute() {
  const { isAuthenticated, isLoading } = useAuth0();
  if (isLoading) return <Loading label="Verificando autenticação..." fullScreen />;
  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <Outlet />;
}

// rotas protegidas: exige login no Auth0 e inicia a sessão no backend.
export function RequireAuth() {
  const { isAuthenticated, isLoading } = useAuth0();
  const location = useLocation();
  if (isLoading) return <Loading label="Verificando autenticação..." fullScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  return (
    <SessionProvider>
      <SessionGate />
    </SessionProvider>
  );
}

function SessionGate() {
  const { session, loading, error, refresh } = useSession();
  const { logout } = useAuth();
  // RNF03: desconecta após o período de inatividade definido pelo backend (15 minutos).
  useIdleLogout(session?.sessionTimeoutMinutes, () => void logout());

  if (loading) return <Loading label="Preparando seu espaço de cuidado..." fullScreen />;
  if (error || !session)
    return (
      <main className="grid min-h-screen place-items-center bg-mist-50 px-4">
        <div className="w-full max-w-md rounded-[var(--radius-card)] border border-mist-200 bg-white p-8 text-center shadow-[var(--shadow-soft)]">
          <Logo width={110} className="mx-auto" />
          <h1 className="mt-4 font-display text-xl font-semibold text-ink">Não foi possível iniciar sua sessão</h1>
          <ErrorMessage error={error} className="mt-4 text-left" />
          <div className="mt-6 flex flex-col gap-2 sm:flex-row sm:justify-center">
            <Button onClick={() => void refresh()} icon={<RefreshCw className="h-4 w-4" />}>Tentar novamente</Button>
            <Button variant="secondary" onClick={() => void logout()} icon={<LogOut className="h-4 w-4" />}>Sair</Button>
          </div>
        </div>
      </main>
    );
  return <Outlet />;
}

// área principal: exige onboarding (RF01) e consentimentos LGPD vigentes (RNF01), como o backend.
export function RequireCompletedProfile() {
  const { session } = useSession();
  if (!session!.user.profileCompleted) return <Navigate to="/onboarding" replace />;
  if (!session!.consentsUpToDate) return <Navigate to="/consentimento" replace />;
  return <Outlet />;
}
