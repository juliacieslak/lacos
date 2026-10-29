// navegação entre páginas de listas paginadas pelo backend.
import { ChevronLeft, ChevronRight } from "lucide-react";

export default function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  if (totalPages <= 1) return null;
  return (
    <nav className="mt-4 flex items-center justify-center gap-3 text-sm text-mist-500" aria-label="Paginação">
      <button type="button" disabled={page <= 1} onClick={() => onChange(page - 1)} className="rounded-lg p-2 hover:bg-lavender-50 disabled:opacity-40" aria-label="Página anterior">
        <ChevronLeft className="h-4 w-4" />
      </button>
      <span>Página {page} de {totalPages}</span>
      <button type="button" disabled={page >= totalPages} onClick={() => onChange(page + 1)} className="rounded-lg p-2 hover:bg-lavender-50 disabled:opacity-40" aria-label="Próxima página">
        <ChevronRight className="h-4 w-4" />
      </button>
    </nav>
  );
}
