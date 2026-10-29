// pacientes: listar, buscar, criar, editar, status, unidades e exclusão definitiva.
import { api } from "./api";
import type { CreatePatientRequest, Patient, PatientCareStatus, PatientSummary, UpdatePatientRequest } from "../types/patient";
import type { GlucoseUnit, TemperatureUnit, WeightUnit } from "../types/healthMeasurement";

const base = "/api/patients";

export const patientService = {
  list: (search?: string, status?: PatientCareStatus, signal?: AbortSignal) =>
    api.get<PatientSummary[]>(base, { search, status }, signal),
  get: (id: string, signal?: AbortSignal) => api.get<Patient>(`${base}/${id}`, undefined, signal),
  create: (data: CreatePatientRequest) => api.post<Patient>(base, data),
  update: (id: string, data: UpdatePatientRequest) => api.put<Patient>(`${base}/${id}`, data),
  changeStatus: (id: string, status: PatientCareStatus, reason?: string) =>
    api.patch<Patient>(`${base}/${id}/status`, { status, reason: reason ?? null }),
  updateUnits: (id: string, temperature: TemperatureUnit, glucose: GlucoseUnit, weight: WeightUnit) =>
    api.put<Patient>(`${base}/${id}/measurement-units`, { temperature, glucose, weight }),
  erase: (id: string, confirmationCode: string, reason: string) => api.delete(`${base}/${id}`, { confirmationCode, reason }),
};
