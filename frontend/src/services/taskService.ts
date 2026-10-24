// rotina de cuidado: tarefas recorrentes e marcação de concluídas.
import { api } from "./api";
import { createRecordService } from "./recordService";
import type { CareTask, CareTaskCompletion, CareTaskRequest } from "../types/careTask";

export const taskService = {
  ...createRecordService<CareTask, CareTaskRequest>("care-routines"),
  complete: (patientId: string, taskId: string, date?: string, notes?: string) =>
    api.post<CareTaskCompletion>(`/api/patients/${patientId}/care-routines/${taskId}/completions`, {
      date: date ?? null,
      notes: notes ?? null,
    }),
  uncomplete: (patientId: string, taskId: string, date: string) =>
    api.delete(`/api/patients/${patientId}/care-routines/${taskId}/completions/${date}`),
};
