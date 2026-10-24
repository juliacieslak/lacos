// tela principal (WF01): resumo agregado calculado pelo backend.
import { api } from "./api";
import type { Dashboard } from "../types/dashboard";

export const dashboardService = {
  get: (patientId?: string, signal?: AbortSignal) => api.get<Dashboard>("/api/dashboard", { patientId }, signal),
};
