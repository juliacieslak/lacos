// item da agenda (consulta, dose de medicamento ou tarefa da rotina) com checkbox de concluído.
import { Check, ClipboardCheck, Loader2, MapPin, Pill, Stethoscope } from "lucide-react";
import Badge from "../ui/badge";
import type { AgendaItem } from "../../types/appointment";
import { formatTime } from "../../utils/formatDate";
import { eventStatusLabels } from "../../utils/labels";

const kindStyle = {
  EVENT: { icon: Stethoscope, tone: "bg-serene-50 border-serene-100", iconTone: "bg-serene-100 text-serene-700" },
  MEDICATION: { icon: Pill, tone: "bg-lavender-50 border-lavender-100", iconTone: "bg-lavender-100 text-lavender-700" },
  CARE_TASK: { icon: ClipboardCheck, tone: "bg-sage-50 border-sage-100", iconTone: "bg-sage-100 text-sage-700" },
};

interface Props {
  item: AgendaItem;
  busy: boolean;
  canToggle: boolean;
  showPatient?: boolean;
  onToggle: (item: AgendaItem) => void;
}

export default function AgendaItemRow({ item, busy, canToggle, showPatient = true, onToggle }: Props) {
  const style = kindStyle[item.kind];
  const Icon = style.icon;
  // compromissos só podem ser marcados como realizados enquanto agendados (no backend, "realizado" é um status final).
  const toggleable = canToggle && (item.kind !== "EVENT" || item.eventStatus === "SCHEDULED");
  return (
    <li className={`flex items-start gap-3 rounded-2xl border p-3 sm:p-4 ${style.tone} ${item.done ? "opacity-75" : ""}`}>
      <span className="w-12 shrink-0 pt-1 text-sm font-semibold text-mist-700">{formatTime(item.startsAt)}</span>
      <button
        type="button"
        disabled={!toggleable || busy}
        onClick={() => onToggle(item)}
        aria-pressed={item.done}
        aria-label={item.done ? `Desfazer: ${item.title}` : `Marcar como feito: ${item.title}`}
        className={`mt-0.5 grid h-6 w-6 shrink-0 place-items-center rounded-md border-2 transition disabled:cursor-not-allowed disabled:opacity-50 ${
          item.done ? "border-sage-700 bg-sage-700 text-white" : "border-mist-300 bg-white hover:border-lavender-500"
        }`}
      >
        {busy ? <Loader2 className="h-3.5 w-3.5 animate-spin text-lavender-600" /> : item.done && <Check className="h-4 w-4" />}
      </button>
      <span className={`hidden h-9 w-9 shrink-0 place-items-center rounded-xl sm:grid ${style.iconTone}`}><Icon className="h-4 w-4" /></span>
      <div className="min-w-0 flex-1">
        <p className={`text-sm font-semibold text-ink ${item.done ? "line-through decoration-mist-400" : ""}`}>{item.title}</p>
        <p className="truncate text-xs text-mist-500">
          {showPatient && <span className="font-medium text-mist-700">{item.patientName}</span>}
          {showPatient && item.subtitle && " · "}
          {item.subtitle}
        </p>
        <div className="mt-1.5 flex flex-wrap gap-1.5">
          {item.overdue && !item.done && <Badge tone="warning">Pendente</Badge>}
          {item.kind === "EVENT" && item.eventStatus && item.eventStatus !== "SCHEDULED" && <Badge tone="serene">{eventStatusLabels[item.eventStatus]}</Badge>}
          {item.administrationStatus === "NOT_ADMINISTERED" && <Badge tone="danger">Não administrado</Badge>}
          {item.locationName && <Badge tone="neutral"><MapPin className="h-3 w-3" /> {item.locationName}</Badge>}
        </div>
      </div>
    </li>
  );
}
