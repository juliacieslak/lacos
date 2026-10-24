// controla carregamento dos pacientes (lista e detalhes).
import { patientService } from "../services/patientService";
import type { PatientCareStatus } from "../types/patient";
import { useAsync } from "./useAsync";

export function usePatients(search = "", status?: PatientCareStatus) {
  return useAsync((signal) => patientService.list(search.trim() || undefined, status, signal), [search, status]);
}

export function usePatient(patientId: string | undefined) {
  return useAsync((signal) => patientService.get(patientId!, signal), [patientId], Boolean(patientId));
}
