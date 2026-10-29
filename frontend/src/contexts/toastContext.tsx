// mensagens rápidas de sucesso/erro (feedback das ações do usuário).
import { CheckCircle2, CircleAlert, X } from "lucide-react";
import { createContext, useCallback, useContext, useState, type ReactNode } from "react";

type ToastKind = "success" | "error";
interface Toast { id: number; kind: ToastKind; message: string }

const ToastContext = createContext<(kind: ToastKind, message: string) => void>(() => {});

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const remove = (id: number) => setToasts((all) => all.filter((t) => t.id !== id));

  const push = useCallback((kind: ToastKind, message: string) => {
    const id = Date.now() + Math.random();
    setToasts((all) => [...all.slice(-3), { id, kind, message }]);
    window.setTimeout(() => remove(id), 4500);
  }, []);

  return (
    <ToastContext.Provider value={push}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 bottom-4 z-[60] flex flex-col items-center gap-2 px-4 sm:items-end" aria-live="polite">
        {toasts.map((t) => (
          <div
            key={t.id}
            role="status"
            className={`pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl border bg-white px-4 py-3 text-sm shadow-[var(--shadow-card)] ${
              t.kind === "success" ? "border-sage-200 text-sage-700" : "border-danger-200 text-danger-600"
            }`}
          >
            {t.kind === "success" ? <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" /> : <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" />}
            <span className="flex-1 text-ink">{t.message}</span>
            <button type="button" onClick={() => remove(t.id)} aria-label="Fechar aviso" className="text-mist-400 hover:text-ink">
              <X className="h-4 w-4" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useToast() {
  const push = useContext(ToastContext);
  return {
    success: (message: string) => push("success", message),
    error: (message: string) => push("error", message),
  };
}
