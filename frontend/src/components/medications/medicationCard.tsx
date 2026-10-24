// detalhes de um medicamento: dose, via, frequência, horários e ações permitidas.
import { CircleCheck, CircleSlash, Pencil, Pill, Square, Trash2 } from "lucide-react";
import type { ReactNode } from "react";
import Badge from "../ui/badge";
import RecordMeta from "../ui/recordMeta";
import type { Permissions } from "../../types/common";
import type { Medication } from "../../types/medication";
import { formatDate, formatTimeOnly } from "../../utils/formatDate";
import { dayLabels, routeLabels } from "../../utils/labels";

interface Props {
  medication: Medication;
  permissions: Permissions;
  onEdit: () => void;
  onDelete: () => void;
  onDiscontinue: () => void;
  onRegisterDose: (administered: boolean) => void;
}

export default function MedicationCard({ medication: m, permissions: p, onEdit, onDelete, onDiscontinue, onRegisterDose }: Props) {
  return (
    <li className="rounded-2xl border border-mist-200 bg-white p-4">
      <div className="flex flex-wrap items-start gap-3">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-lavender-100 text-lavender-600"><Pill className="h-5 w-5" /></span>
        <div className="min-w-0 flex-1">
          <p className="font-display font-semibold text-ink">{m.name} <span className="font-sans text-sm font-normal text-mist-500">{m.dosage}</span></p>
          <p className="text-xs text-mist-500">
            {routeLabels[m.administrationRoute]} · {m.frequency}{m.brand && ` · ${m.brand}`}{m.prescribedBy && ` · Prescrito por ${m.prescribedBy}`}
          </p>
          <div className="mt-2 flex flex-wrap gap-1.5">
            <Badge tone={m.isActiveToday ? "sage" : "neutral"}>{m.isActiveToday ? "Em uso hoje" : "Fora do período/dia"}</Badge>
            {m.administrationTimes.map((t) => <Badge key={t} tone="lavender">{formatTimeOnly(t)}</Badge>)}
            {m.daysOfWeek.length > 0 && <Badge tone="serene">{m.daysOfWeek.map((d) => dayLabels[d]).join(", ")}</Badge>}
          </div>
          <p className="mt-2 text-xs text-mist-500">De {formatDate(m.startDate)}{m.endDate ? ` até ${formatDate(m.endDate)}` : " (uso contínuo)"}{m.usageReason && ` · Motivo: ${m.usageReason}`}</p>
          <div className="mt-2"><RecordMeta record={m.record} /></div>
        </div>
        <div className="flex gap-1">
          {p.canEdit && <IconButton label="Editar" onClick={onEdit}><Pencil className="h-4 w-4" /></IconButton>}
          {p.canDelete && <IconButton label="Excluir" onClick={onDelete} danger><Trash2 className="h-4 w-4" /></IconButton>}
        </div>
      </div>
      {(p.canAdd || p.canEdit) && (
        <div className="mt-3 flex flex-wrap gap-2 border-t border-mist-100 pt-3">
          {p.canAdd && (
            <>
              <button type="button" onClick={() => onRegisterDose(true)} className="inline-flex items-center gap-1.5 rounded-lg bg-sage-100 px-3 py-1.5 text-xs font-semibold text-sage-700 hover:bg-sage-200">
                <CircleCheck className="h-3.5 w-3.5" /> Registrar dose agora
              </button>
              <button type="button" onClick={() => onRegisterDose(false)} className="inline-flex items-center gap-1.5 rounded-lg bg-mist-100 px-3 py-1.5 text-xs font-semibold text-mist-700 hover:bg-mist-200">
                <CircleSlash className="h-3.5 w-3.5" /> Não administrado
              </button>
            </>
          )}
          {p.canEdit && !m.endDate && (
            <button type="button" onClick={onDiscontinue} className="inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold text-mist-500 hover:bg-mist-100">
              <Square className="h-3.5 w-3.5" /> Encerrar uso
            </button>
          )}
        </div>
      )}
    </li>
  );
}

function IconButton({ label, onClick, danger, children }: { label: string; onClick: () => void; danger?: boolean; children: ReactNode }) {
  return (
    <button type="button" onClick={onClick} aria-label={label} title={label}
      className={`rounded-lg p-2 text-mist-400 transition ${danger ? "hover:bg-danger-50 hover:text-danger-600" : "hover:bg-lavender-50 hover:text-lavender-700"}`}>
      {children}
    </button>
  );
}
