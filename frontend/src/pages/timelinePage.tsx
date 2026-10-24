// aba "Histórico": linha do tempo clínica (US04: ordem decrescente, filtro por categoria, quem registrou)
// e histórico de inclusões/alterações/exclusões com o responsável (RF06/RF10).
import { History, ListFilter } from "lucide-react";
import { useState } from "react";
import Badge from "../components/ui/badge";
import { Card, CardHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Pagination from "../components/ui/pagination";
import { usePatientContext } from "../contexts/patientContext";
import { useAsync } from "../hooks/useAsync";
import { historyService } from "../services/historyService";
import type { AuditAction, TimelineCategory } from "../types/history";
import { formatDateTime } from "../utils/formatDate";
import { timelineCategoryLabels } from "../utils/labels";

const actionLabels: Record<AuditAction, string> = {
  CREATE: "Inclusão", UPDATE: "Alteração", DELETE: "Exclusão", STATUS_CHANGE: "Mudança de status",
  PROFILE_COMPLETED: "Cadastro concluído", VIEW: "Visualização", ERASURE: "Exclusão definitiva",
};

export default function TimelinePage() {
  const { patient } = usePatientContext();
  const [tab, setTab] = useState<"timeline" | "changes">("timeline");
  const [categories, setCategories] = useState<TimelineCategory[]>([]);
  const [page, setPage] = useState(1);
  const timeline = useAsync((s) => historyService.timeline(patient.id, categories, page, s), [patient.id, categories, page], tab === "timeline");
  const changes = useAsync((s) => historyService.changes(patient.id, page, s), [patient.id, page], tab === "changes");

  const toggleCategory = (c: TimelineCategory) => {
    setPage(1);
    setCategories((all) => (all.includes(c) ? all.filter((x) => x !== c) : [...all, c]));
  };

  return (
    <Card>
      <CardHeader title="Histórico do paciente" icon={<History className="h-4 w-4" />}
        action={
          <div className="flex rounded-xl bg-mist-100 p-1 text-sm">
            {(["timeline", "changes"] as const).map((t) => (
              <button key={t} type="button" onClick={() => { setTab(t); setPage(1); }} aria-pressed={tab === t}
                className={`rounded-lg px-3 py-1.5 font-semibold ${tab === t ? "bg-white text-lavender-700 shadow-sm" : "text-mist-500"}`}>
                {t === "timeline" ? "Linha do tempo" : "Alterações"}
              </button>
            ))}
          </div>
        } />

      {tab === "timeline" ? (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-1.5">
            <ListFilter className="h-4 w-4 text-mist-400" aria-hidden />
            {(Object.keys(timelineCategoryLabels) as TimelineCategory[]).map((c) => (
              <button key={c} type="button" onClick={() => toggleCategory(c)} aria-pressed={categories.includes(c)}
                className={`rounded-full px-3 py-1 text-xs font-semibold transition ${categories.includes(c) ? "bg-lavender-600 text-white" : "bg-mist-100 text-mist-500 hover:bg-lavender-50"}`}>
                {timelineCategoryLabels[c]}
              </button>
            ))}
          </div>
          {timeline.loading && !timeline.data ? <Loading /> : timeline.error ? <ErrorMessage error={timeline.error} onRetry={timeline.reload} /> : !timeline.data?.items.length ? (
            <EmptyState title="Nenhum registro encontrado" />
          ) : (
            <>
              <ol className="relative space-y-4 border-l-2 border-lavender-200 pl-5">
                {timeline.data.items.map((i) => (
                  <li key={`${i.category}:${i.entityId}`} className="relative">
                    <span className="absolute -left-[27px] top-1.5 h-3 w-3 rounded-full border-2 border-white bg-lavender-500" aria-hidden />
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone="lavender">{timelineCategoryLabels[i.category]}</Badge>
                      <span className="text-xs text-mist-500">{formatDateTime(i.occurredAt)}</span>
                    </div>
                    <p className="mt-1 text-sm font-semibold text-ink">{i.title}</p>
                    {i.description && <p className="text-sm text-mist-700">{i.description}</p>}
                    <p className="text-[11px] text-mist-400">Registrado por {i.recordedByName ?? "—"}</p>
                  </li>
                ))}
              </ol>
              <Pagination page={timeline.data.page} totalPages={timeline.data.totalPages} onChange={setPage} />
            </>
          )}
        </>
      ) : changes.loading && !changes.data ? <Loading /> : changes.error ? <ErrorMessage error={changes.error} onRetry={changes.reload} /> : !changes.data?.items.length ? (
        <EmptyState title="Nenhuma alteração registrada" />
      ) : (
        <>
          <ul className="divide-y divide-mist-100">
            {changes.data.items.map((c) => (
              <li key={c.id} className="py-3">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge tone={c.action === "DELETE" || c.action === "ERASURE" ? "danger" : c.action === "CREATE" ? "sage" : "serene"}>{actionLabels[c.action]}</Badge>
                  <span className="text-sm font-semibold text-ink">{c.entityType.replaceAll("_", " ").toLowerCase()}</span>
                  <span className="text-xs text-mist-500">por {c.userName ?? "Sistema"} em {formatDateTime(c.createdAt)}</span>
                </div>
                {c.newValues && (
                  <p className="mt-1 break-words text-xs text-mist-500">
                    {Object.entries(c.newValues).slice(0, 6).map(([k, v]) => `${k}: ${typeof v === "object" ? JSON.stringify(v) : String(v)}`).join(" · ")}
                  </p>
                )}
                {c.reason && <p className="mt-1 text-xs text-mist-700">Motivo: {c.reason}</p>}
              </li>
            ))}
          </ul>
          <Pagination page={changes.data.page} totalPages={changes.data.totalPages} onChange={setPage} />
        </>
      )}
    </Card>
  );
}
