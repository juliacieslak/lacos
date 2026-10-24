// dados do paciente aberto, compartilhados entre as abas da página de detalhes (via Outlet do React Router).
import { useOutletContext } from "react-router-dom";
import type { Patient } from "../types/patient";

export interface PatientOutletContext {
  patient: Patient;
  reloadPatient: () => void;
}

export function usePatientContext() {
  return useOutletContext<PatientOutletContext>();
}
