// ações rápidas da agenda (pedido do cuidador na validação da Sprint 3):
// marcar medicação como administrada, tarefa da rotina como feita e consulta como realizada — e desfazer.
import { useCallback, useState } from "react";
import { appointmentService } from "../services/appointmentService";
import { medicationService } from "../services/medicationService";
import { taskService } from "../services/taskService";
import { useToast } from "../contexts/toastContext";
import { toApiError } from "./useAsync";
import type { AgendaItem } from "../types/appointment";

export function useAgendaActions(onChanged: () => void) {
  const toast = useToast();
  const [busyKey, setBusyKey] = useState<string | null>(null);

  const toggle = useCallback(async (item: AgendaItem) => {
    const key = `${item.kind}:${item.sourceId}:${item.startsAt}`;
    setBusyKey(key);
    try {
      if (item.kind === "MEDICATION") {
        if (item.done && item.administrationId) await medicationService.administrations.remove(item.patientId, item.administrationId);
        else
          await medicationService.administrations.create(item.patientId, {
            medicationId: item.sourceId, scheduledFor: item.startsAt, administeredAt: null, status: "ADMINISTERED", notes: null,
          });
      } else if (item.kind === "CARE_TASK") {
        if (item.done) await taskService.uncomplete(item.patientId, item.sourceId, item.taskDate!);
        else await taskService.complete(item.patientId, item.sourceId, item.taskDate!);
      } else {
        await appointmentService.changeStatus(item.patientId, item.sourceId, "COMPLETED");
      }
      toast.success(item.done ? "Marcação desfeita." : "Registrado como concluído.");
      onChanged();
    } catch (err) {
      toast.error(toApiError(err).message);
    } finally {
      setBusyKey(null);
    }
  }, [onChanged, toast]);

  return { toggle, busyKey };
}
