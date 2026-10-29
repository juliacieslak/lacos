// sessão do usuário no backend: registra o usuário no primeiro acesso (POST /api/auth/session)
// e disponibiliza dados do perfil e situação de acesso para toda a área autenticada.
import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import { sessionService } from "../services/sessionService";
import { toApiError } from "../hooks/useAsync";
import type { ApiError } from "../services/api";
import type { Session, User } from "../types/user";

interface SessionContextValue {
  session: Session | null;
  loading: boolean;
  error: ApiError | null;
  refresh: () => Promise<void>;
  updateUser: (user: User) => void;
}

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  const refresh = useCallback(async () => {
    setError(null);
    try {
      setSession(await sessionService.start());
    } catch (err) {
      setError(toApiError(err));
    } finally {
      setLoading(false);
    }
  }, []);

  // carga inicial: o estado só é alterado quando a resposta chega (sem setState síncrono no efeito).
  useEffect(() => {
    let active = true;
    sessionService
      .start()
      .then((s) => active && setSession(s))
      .catch((err) => active && setError(toApiError(err)))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, []);

  const updateUser = useCallback((user: User) => setSession((s) => (s ? { ...s, user } : s)), []);

  return <SessionContext.Provider value={{ session, loading, error, refresh, updateUser }}>{children}</SessionContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useSession() {
  const ctx = useContext(SessionContext);
  if (!ctx) throw new Error("useSession deve ser usado dentro de SessionProvider.");
  return ctx;
}
