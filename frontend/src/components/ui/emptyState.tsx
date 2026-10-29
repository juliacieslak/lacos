// mensagem exibida quando não há dados.
import { Inbox } from "lucide-react";
import type { ReactNode } from "react";

interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
}

export default function EmptyState({ title, description, icon, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center rounded-[var(--radius-card)] border border-dashed border-mist-300 bg-white/60 px-6 py-10 text-center">
      <div className="mb-3 grid h-12 w-12 place-items-center rounded-full bg-lavender-100 text-lavender-600">
        {icon ?? <Inbox className="h-5 w-5" />}
      </div>
      <p className="font-display font-semibold text-ink">{title}</p>
      {description && <p className="mt-1 max-w-md text-sm text-mist-500">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
