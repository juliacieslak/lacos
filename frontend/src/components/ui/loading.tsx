// indicador exibido enquanto os dados estão carregando.
import { Loader2 } from "lucide-react";

export default function Loading({ label = "Carregando...", fullScreen = false }: { label?: string; fullScreen?: boolean }) {
  return (
    <div role="status" className={`flex items-center justify-center gap-3 text-sm text-mist-500 ${fullScreen ? "min-h-screen bg-mist-50" : "py-12"}`}>
      <Loader2 className="h-5 w-5 animate-spin text-lavender-500" aria-hidden />
      <span>{label}</span>
    </div>
  );
}
