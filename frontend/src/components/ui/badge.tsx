// etiqueta compacta para status, permissões e categorias.
import type { ReactNode } from "react";

export type BadgeTone = "lavender" | "sage" | "serene" | "neutral" | "danger" | "warning";

const tones: Record<BadgeTone, string> = {
  lavender: "bg-lavender-100 text-lavender-700",
  sage: "bg-sage-100 text-sage-700",
  serene: "bg-serene-100 text-serene-700",
  neutral: "bg-mist-100 text-mist-700",
  danger: "bg-danger-50 text-danger-600",
  warning: "bg-warning-50 text-warning-700",
};

export default function Badge({ tone = "neutral", children, className = "" }: { tone?: BadgeTone; children: ReactNode; className?: string }) {
  return <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium ${tones[tone]} ${className}`}>{children}</span>;
}
