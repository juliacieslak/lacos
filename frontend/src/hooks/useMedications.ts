// controla medicamentos do paciente.
import { medicationService } from "../services/medicationService";
import { useAsync } from "./useAsync";

export function useMedications(patientId: string, activeOnly: boolean) {
  return useAsync((signal) => medicationService.current(patientId, activeOnly, signal), [patientId, activeOnly]);
}
