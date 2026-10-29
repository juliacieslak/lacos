// agenda de cuidados (WF03): compromissos, medicações e tarefas do dia, semana ou mês, de um ou de todos os pacientes.
import { CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";
import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import AgendaItemRow from "../components/agenda/agendaItemRow";
import { Card, PageHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Select from "../components/ui/select";
import { useAgendaActions } from "../hooks/useAgendaActions";
import { useAsync } from "../hooks/useAsync";
import { usePatients } from "../hooks/usePatients";
import { appointmentService } from "../services/appointmentService";
import type { AgendaItem, AgendaView } from "../types/appointment";
import { addDays, formatDate, formatLongDate, todayIso } from "../utils/formatDate";

const views: { value: AgendaView; label: string }[] = [
  { value: "DAY", label: "Dia" },
  { value: "WEEK", label: "Semana" },
  { value: "MONTH", label: "Mês" },
];

export default function AgendaPage() {
  const [params, setParams] = useSearchParams();
  const patientId = params.get("paciente") ?? "";
  const [date, setDate] = useState(todayIso());
  const [view, setView] = useState<AgendaView>("DAY");
  const patients = usePatients();
  const agenda = useAsync((s) => appointmentService.agenda({ patientId: patientId || undefined, date, view }, s), [patientId, date, view]);
  const { toggle, busyKey } = useAgendaActions(agenda.reload);

  // navegação de datas: avança/volta um dia, uma semana ou um mês.
  const move = (direction: 1 | -1) => {
    if (view === "DAY") setDate(addDays(date, direction));
    else if (view === "WEEK") setDate(addDays(date, 7 * direction));
    else {
      const [y, m] = date.split("-").map(Number);
      const d = new Date(y, m - 1 + direction, 1);
      setDate(todayIso(d));
    }
  };

  // agrupa por dia para as visões de semana e mês.
  const groups = useMemo(() => {
    const map = new Map<string, AgendaItem[]>();
    (agenda.data?.items ?? []).forEach((i) => {
      const key = todayIso(new Date(i.startsAt));
      map.set(key, [...(map.get(key) ?? []), i]);
    });
    return [...map.entries()];
  }, [agenda.data]);

  const canAdd = (id: string) => patients.data?.find((p) => p.id === id)?.myPermissions.canAdd ?? false;
  const title = view === "DAY" ? formatLongDate(date) : agenda.data ? `${formatDate(agenda.data.from)} a ${formatDate(agenda.data.to)}` : "";

  return (
    <>
      <PageHeader eyebrow="Agenda" title="Agenda de cuidados" description="Acompanhe os compromissos, medicações e cuidados do dia."
        action={
          <Select ariaLabel="Paciente" value={patientId} onChange={(e) => setParams(e.target.value ? { paciente: e.target.value } : {})}
            options={[{ value: "", label: "Todos os pacientes" }, ...(patients.data ?? []).map((p) => ({ value: p.id, label: p.fullName }))]} />
        } />

      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-1 rounded-xl border border-mist-200 bg-white p-1">
          <button type="button" onClick={() => move(-1)} className="rounded-lg p-2 hover:bg-lavender-50" aria-label="Anterior"><ChevronLeft className="h-4 w-4" /></button>
          <span className="flex items-center gap-2 px-2 text-sm font-semibold text-ink"><CalendarDays className="h-4 w-4 text-lavender-500" />{title}</span>
          <button type="button" onClick={() => move(1)} className="rounded-lg p-2 hover:bg-lavender-50" aria-label="Próximo"><ChevronRight className="h-4 w-4" /></button>
          <button type="button" onClick={() => setDate(todayIso())} className="rounded-lg px-3 py-1.5 text-xs font-semibold text-lavender-600 hover:bg-lavender-50">Hoje</button>
        </div>
        <div className="flex rounded-xl bg-mist-100 p-1 text-sm" role="group" aria-label="Visualização">
          {views.map((v) => (
            <button key={v.value} type="button" aria-pressed={view === v.value} onClick={() => setView(v.value)}
              className={`rounded-lg px-4 py-1.5 font-semibold ${view === v.value ? "bg-lavender-600 text-white" : "text-mist-500"}`}>{v.label}</button>
          ))}
        </div>
      </div>

      <Card>
        {agenda.loading && !agenda.data ? <Loading /> : agenda.error ? <ErrorMessage error={agenda.error} onRetry={agenda.reload} /> : groups.length === 0 ? (
          <EmptyState title="Nada programado para este período" description="Medicamentos com horário, tarefas da rotina e compromissos aparecem aqui." />
        ) : (
          <div className="space-y-6">
            {groups.map(([day, items]) => (
              <section key={day}>
                {view !== "DAY" && <h2 className="mb-2 text-sm font-semibold text-mist-700">{formatLongDate(day)}</h2>}
                <ul className="space-y-2">
                  {items.map((item) => (
                    <AgendaItemRow key={`${item.kind}:${item.sourceId}:${item.startsAt}`} item={item} showPatient={!patientId}
                      canToggle={canAdd(item.patientId)} busy={busyKey === `${item.kind}:${item.sourceId}:${item.startsAt}`} onToggle={toggle} />
                  ))}
                </ul>
              </section>
            ))}
          </div>
        )}
      </Card>
    </>
  );
}
