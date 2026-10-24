// medicamentos (RF08) e registro de doses administradas (US02).
import { api, apiRequest } from "./api";
import { createRecordService } from "./recordService";
import type {
  Medication,
  MedicationAdministration,
  MedicationAdministrationRequest,
  MedicationRequest,
} from "../types/medication";

const crud = createRecordService<Medication, MedicationRequest>("medications");

export const medicationService = {
  ...crud,
  current: (patientId: string, activeOnly = true, signal?: AbortSignal) =>
    api.get<Medication[]>(`/api/patients/${patientId}/medications/current`, { activeOnly }, signal),
  discontinue: (patientId: string, id: string, endDate?: string) =>
    apiRequest<Medication>(`/api/patients/${patientId}/medications/${id}/discontinue`, { method: "POST", query: { endDate } }),
  administrations: createRecordService<MedicationAdministration, MedicationAdministrationRequest>("medication-administrations"),
};
