// mensagem padrão para erros (com opção de tentar novamente).
import { CircleAlert, RefreshCw } from "lucide-react";
import type { ApiError } from "../../services/api";

interface ErrorMessageProps {
  error: ApiError | string | null | undefined;
  onRetry?: () => void;
  className?: string;
}

export default function ErrorMessage({ error, onRetry, className = "" }: ErrorMessageProps) {
  if (!error) return null;
  const message = typeof error === "string" ? error : error.message;
  return (
    <div role="alert" className={`flex items-start gap-3 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-600 ${className}`}>
      <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
      <p className="flex-1">{message}</p>
      {onRetry && (
        <button type="button" onClick={onRetry} className="inline-flex shrink-0 items-center gap-1 font-semibold hover:underline">
          <RefreshCw className="h-3.5 w-3.5" /> Tentar novamente
        </button>
      )}
    </div>
  );
}
