// controla as tarefas da rotina de cuidado do paciente.
import { taskService } from "../services/taskService";
import { useAsync } from "./useAsync";

export function useTasks(patientId: string) {
  return useAsync((signal) => taskService.list(patientId, { pageSize: 100 }, signal), [patientId]);
}
