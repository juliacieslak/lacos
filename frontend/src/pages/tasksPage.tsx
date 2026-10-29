// aba "Rotina": tarefas recorrentes de cuidado e marcação do dia (conclusões aparecem na agenda).
import { Plus } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import TaskForm from "../components/tasks/taskForm";
import TaskList from "../components/tasks/taskList";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import { usePatientContext } from "../contexts/patientContext";
import { useToast } from "../contexts/toastContext";
import { useConfirmDelete } from "../hooks/useConfirmDelete";
import { useTasks } from "../hooks/useTasks";
import { taskService } from "../services/taskService";
import type { CareTask, CareTaskRequest } from "../types/careTask";

export default function TasksPage() {
  const { patient } = usePatientContext();
  const toast = useToast();
  const [editing, setEditing] = useState<CareTask | "new" | null>(null);
  const { data, loading, error, reload } = useTasks(patient.id);
  const del = useConfirmDelete((t: CareTask) => taskService.remove(patient.id, t.id), reload, "esta tarefa");

  const save = async (form: CareTaskRequest) => {
    if (editing === "new") await taskService.create(patient.id, form);
    else if (editing) await taskService.update(patient.id, editing.id, form);
    toast.success("Rotina atualizada.");
    setEditing(null);
    reload();
  };

  return (
    <Card>
      <CardHeader title="Rotina de cuidado" subtitle="Para marcar as tarefas de hoje como feitas, use a agenda."
        action={
          <div className="flex gap-2">
            <Link to={`/agenda?paciente=${patient.id}`}><Button variant="secondary">Abrir agenda</Button></Link>
            {patient.myPermissions.canAdd && <Button icon={<Plus className="h-4 w-4" />} onClick={() => setEditing("new")}>Adicionar cuidado</Button>}
          </div>
        } />
      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : (
        <TaskList tasks={data?.items ?? []} permissions={patient.myPermissions} onEdit={setEditing} onDelete={del.request} />
      )}
      <Modal open={editing !== null} title={editing === "new" ? "Adicionar cuidado" : "Editar cuidado"} size="lg" onClose={() => setEditing(null)}>
        {editing !== null && <TaskForm task={editing === "new" ? undefined : editing} onSubmit={save} onCancel={() => setEditing(null)} />}
      </Modal>
      {del.element}
    </Card>
  );
}
