// exibe uma tarefa da rotina (horário, recorrência e instruções) com ações de editar e excluir.
import { Clock, Pencil, Trash2 } from "lucide-react";
import Badge from "../ui/badge";
import RecordMeta from "../ui/recordMeta";
import type { CareTask } from "../../types/careTask";
import type { Permissions } from "../../types/common";
import { formatDate, formatTimeOnly } from "../../utils/formatDate";
import { careCategoryLabels, dayLabels } from "../../utils/labels";

export default function TaskCard({ task, permissions, onEdit, onDelete }: { task: CareTask; permissions: Permissions; onEdit: () => void; onDelete: () => void }) {
  return (
    <li className="flex items-start gap-3 rounded-2xl border border-mist-200 bg-white p-4">
      <span className="grid h-10 w-14 shrink-0 place-items-center rounded-xl bg-sage-100 text-sm font-semibold text-sage-700">{formatTimeOnly(task.scheduledTime)}</span>
      <div className="min-w-0 flex-1">
        <p className="font-semibold text-ink">{task.title}</p>
        {task.description && <p className="text-sm text-mist-500">{task.description}</p>}
        <div className="mt-2 flex flex-wrap gap-1.5">
          <Badge tone="sage">{careCategoryLabels[task.category]}</Badge>
          <Badge tone="neutral"><Clock className="h-3 w-3" /> {task.daysOfWeek.length ? task.daysOfWeek.map((d) => dayLabels[d]).join(", ") : "Todos os dias"}</Badge>
          {task.endDate && <Badge tone="neutral">Até {formatDate(task.endDate)}</Badge>}
        </div>
        <div className="mt-2"><RecordMeta record={task.record} /></div>
      </div>
      <div className="flex gap-1">
        {permissions.canEdit && <button type="button" onClick={onEdit} aria-label="Editar tarefa" className="rounded-lg p-2 text-mist-400 hover:bg-lavender-50 hover:text-lavender-700"><Pencil className="h-4 w-4" /></button>}
        {permissions.canDelete && <button type="button" onClick={onDelete} aria-label="Excluir tarefa" className="rounded-lg p-2 text-mist-400 hover:bg-danger-50 hover:text-danger-600"><Trash2 className="h-4 w-4" /></button>}
      </div>
    </li>
  );
}
