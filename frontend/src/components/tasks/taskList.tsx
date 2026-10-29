// lista as tarefas da rotina do paciente.
import { ClipboardCheck } from "lucide-react";
import TaskCard from "./taskCard";
import EmptyState from "../ui/emptyState";
import type { CareTask } from "../../types/careTask";
import type { Permissions } from "../../types/common";

export default function TaskList({ tasks, permissions, onEdit, onDelete }: { tasks: CareTask[]; permissions: Permissions; onEdit: (t: CareTask) => void; onDelete: (t: CareTask) => void }) {
  if (tasks.length === 0)
    return <EmptyState icon={<ClipboardCheck className="h-5 w-5" />} title="Nenhuma tarefa na rotina" description="Cadastre cuidados recorrentes, como alimentação, higiene ou verificação de sinais vitais." />;
  return <ul className="space-y-3">{[...tasks].sort((a, b) => a.scheduledTime.localeCompare(b.scheduledTime)).map((t) => <TaskCard key={t.id} task={t} permissions={permissions} onEdit={() => onEdit(t)} onDelete={() => onDelete(t)} />)}</ul>;
}
