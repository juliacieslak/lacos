// compromissos (consultas, exames, tratamentos - RF18) e agenda consolidada (WF03).
import { api } from "./api";
import { createRecordService } from "./recordService";
import type { Agenda, AgendaView, Appointment, AppointmentRequest, EventStatus } from "../types/appointment";

export const appointmentService = {
  ...createRecordService<Appointment, AppointmentRequest>("events"),
  changeStatus: (patientId: string, id: string, status: EventStatus, summary?: string | null) =>
    api.patch<Appointment>(`/api/patients/${patientId}/events/${id}/status`, { status, summary: summary ?? null }),
  setSummary: (patientId: string, id: string, summary: string | null) =>
    api.put<Appointment>(`/api/patients/${patientId}/events/${id}/summary`, { summary }),
  agenda: (params: { patientId?: string; date?: string; view: AgendaView }, signal?: AbortSignal) =>
    api.get<Agenda>("/api/agenda", params, signal),
};
