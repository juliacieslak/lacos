// aba "Compromissos": consultas, exames, tratamentos e outras atividades (RF18), com status e resumo da ocasião.
import { CalendarClock, CheckCircle2, MapPin, Pencil, Plus, Trash2, UserRound } from "lucide-react";
import { useState } from "react";
import SimpleForm from "../components/records/simpleForm";
import Badge from "../components/ui/badge";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import Pagination from "../components/ui/pagination";
import RecordMeta from "../components/ui/recordMeta";
import { usePatientContext } from "../contexts/patientContext";
import { useToast } from "../contexts/toastContext";
import { useAsync } from "../hooks/useAsync";
import { useConfirmDelete } from "../hooks/useConfirmDelete";
import { appointmentService } from "../services/appointmentService";
import type { Appointment, AppointmentRequest, EventStatus, EventType } from "../types/appointment";
import { formatDateTime, fromDateTimeLocal, toDateTimeLocal } from "../utils/formatDate";
import { eventStatusLabels, eventTypeLabels, toOptions } from "../utils/labels";

const statusTone: Record<EventStatus, "serene" | "sage" | "neutral" | "danger"> = { SCHEDULED: "serene", COMPLETED: "sage", CANCELLED: "neutral", MISSED: "danger" };

export default function AppointmentsPage() {
  const { patient } = usePatientContext();
  const toast = useToast();
  const p = patient.myPermissions;
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<Appointment | "new" | null>(null);
  const [statusTarget, setStatusTarget] = useState<Appointment | null>(null);
  const { data, loading, error, reload } = useAsync((s) => appointmentService.list(patient.id, { page, pageSize: 10 }, s), [patient.id, page]);
  const del = useConfirmDelete((a: Appointment) => appointmentService.remove(patient.id, a.id), reload, "este compromisso");

  const save = async (form: AppointmentRequest) => {
    if (editing === "new") await appointmentService.create(patient.id, form);
    else if (editing) await appointmentService.update(patient.id, editing.id, form);
    toast.success("Compromisso salvo.");
    setEditing(null);
    reload();
  };

  const changeStatus = async (form: { status: EventStatus; summary: string | null }) => {
    await appointmentService.changeStatus(patient.id, statusTarget!.id, form.status, form.summary);
    toast.success("Status atualizado.");
    setStatusTarget(null);
    reload();
  };

  return (
    <Card>
      <CardHeader title="Compromissos" subtitle="Ordenados do mais recente para o mais antigo" icon={<CalendarClock className="h-4 w-4" />}
        action={p.canAdd && <Button icon={<Plus className="h-4 w-4" />} onClick={() => setEditing("new")}>Novo compromisso</Button>} />
      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : !data || data.items.length === 0 ? (
        <EmptyState title="Nenhum compromisso cadastrado" description="Registre consultas, exames e tratamentos do paciente." />
      ) : (
        <>
          <ul className="space-y-3">
            {data.items.map((a) => (
              <li key={a.id} className="rounded-2xl border border-mist-200 bg-white p-4">
                <div className="flex flex-wrap items-start gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone="lavender">{eventTypeLabels[a.eventType]}</Badge>
                      <Badge tone={statusTone[a.status]}>{eventStatusLabels[a.status]}</Badge>
                    </div>
                    <p className="mt-1.5 font-semibold text-ink">{a.title}</p>
                    <p className="text-sm text-mist-500">{formatDateTime(a.scheduledAt)}{a.durationMinutes && ` · ${a.durationMinutes} min`}</p>
                    <div className="mt-1 flex flex-wrap gap-x-4 text-xs text-mist-500">
                      {a.providerName && <span className="inline-flex items-center gap-1"><UserRound className="h-3 w-3" /> {a.providerName}</span>}
                      {a.locationName && <span className="inline-flex items-center gap-1"><MapPin className="h-3 w-3" /> {a.locationName}</span>}
                    </div>
                    {a.notes && <p className="mt-2 text-sm text-mist-700">{a.notes}</p>}
                    {a.summary && <p className="mt-2 rounded-xl bg-sage-50 p-3 text-sm text-ink"><strong>Resumo:</strong> {a.summary}</p>}
                    <div className="mt-2"><RecordMeta record={a.record} /></div>
                  </div>
                  <div className="flex gap-1">
                    {p.canEdit && <button type="button" onClick={() => setStatusTarget(a)} aria-label="Atualizar status" title="Atualizar status" className="rounded-lg p-2 text-mist-400 hover:bg-sage-50 hover:text-sage-700"><CheckCircle2 className="h-4 w-4" /></button>}
                    {p.canEdit && <button type="button" onClick={() => setEditing(a)} aria-label="Editar" className="rounded-lg p-2 text-mist-400 hover:bg-lavender-50 hover:text-lavender-700"><Pencil className="h-4 w-4" /></button>}
                    {p.canDelete && <button type="button" onClick={() => del.request(a)} aria-label="Excluir" className="rounded-lg p-2 text-mist-400 hover:bg-danger-50 hover:text-danger-600"><Trash2 className="h-4 w-4" /></button>}
                  </div>
                </div>
              </li>
            ))}
          </ul>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      )}

      <Modal open={editing !== null} title={editing === "new" ? "Novo compromisso" : "Editar compromisso"} size="lg" onClose={() => setEditing(null)}>
        {editing !== null && (() => {
          const item = editing === "new" ? undefined : editing;
          return (
            <SimpleForm onSubmit={save} onCancel={() => setEditing(null)}
              fields={[
                { name: "eventType", label: "Tipo", type: "select", options: toOptions(eventTypeLabels) },
                { name: "scheduledAt", label: "Data e hora", type: "datetime", required: true },
                { name: "title", label: "Descrição", type: "text", required: true, maxLength: 150, wide: true, placeholder: "Ex.: Consulta com cardiologista" },
                { name: "durationMinutes", label: "Duração (minutos)", type: "text", placeholder: "Ex.: 30" },
                { name: "providerName", label: "Profissional responsável", type: "text", maxLength: 150 },
                { name: "locationName", label: "Local", type: "text", maxLength: 200, wide: true },
                { name: "notes", label: "Anotações", type: "textarea", maxLength: 4000, wide: true },
              ]}
              initial={{
                eventType: item?.eventType ?? "APPOINTMENT", scheduledAt: toDateTimeLocal(item?.scheduledAt), title: item?.title ?? "",
                durationMinutes: item?.durationMinutes?.toString() ?? "", providerName: item?.providerName ?? "", locationName: item?.locationName ?? "", notes: item?.notes ?? "",
              }}
              toRequest={(v): AppointmentRequest => ({
                eventType: v.eventType as EventType, title: v.title, scheduledAt: fromDateTimeLocal(v.scheduledAt) ?? "",
                durationMinutes: v.durationMinutes ? Number(v.durationMinutes) : null, providerName: v.providerName || null, locationName: v.locationName || null, notes: v.notes || null,
              })} />
          );
        })()}
      </Modal>

      <Modal open={statusTarget !== null} title="Atualizar status" onClose={() => setStatusTarget(null)}>
        {statusTarget && (
          <SimpleForm onSubmit={changeStatus} onCancel={() => setStatusTarget(null)}
            fields={[
              { name: "status", label: "Status", type: "select", options: toOptions(eventStatusLabels), wide: true,
                hint: "Agendado → realizado/cancelado/não compareceu; cancelado/não compareceu → reagendado." },
              { name: "summary", label: "Resumo da ocasião / resultado", type: "textarea", maxLength: 4000, wide: true },
            ]}
            initial={{ status: statusTarget.status, summary: statusTarget.summary ?? "" }}
            toRequest={(v) => ({ status: v.status as EventStatus, summary: v.summary || null })} />
        )}
      </Modal>
      {del.element}
    </Card>
  );
}
