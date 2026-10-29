// superfície padrão (card) e cabeçalho de seção.
import type { ReactNode } from "react";

export function Card({ children, className = "" }: { children: ReactNode; className?: string }) {
  return <section className={`rounded-[var(--radius-card)] border border-mist-200 bg-white p-5 shadow-[var(--shadow-card)] ${className}`}>{children}</section>;
}

export function CardHeader({ title, subtitle, action, icon }: { title: string; subtitle?: string; action?: ReactNode; icon?: ReactNode }) {
  return (
    <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
      <div className="flex items-center gap-3">
        {icon && <span className="grid h-9 w-9 place-items-center rounded-xl bg-lavender-100 text-lavender-600">{icon}</span>}
        <div>
          <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
          {subtitle && <p className="text-xs text-mist-500">{subtitle}</p>}
        </div>
      </div>
      {action}
    </div>
  );
}

export function PageHeader({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        {eyebrow && <p className="text-xs font-medium uppercase tracking-[0.08em] text-lavender-600">{eyebrow}</p>}
        <h1 className="font-display text-2xl font-semibold tracking-[-0.01em] text-ink sm:text-[28px]">{title}</h1>
        {description && <p className="mt-1 text-sm text-mist-500">{description}</p>}
      </div>
      {action}
    </div>
  );
}
