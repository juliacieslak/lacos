import type { IsoDate, IsoDateTime, RecordInfo } from "./common";
import type { AdministrationStatus } from "./medication";

export type EventType = "APPOINTMENT" | "EXAM" | "TREATMENT" | "OTHER";
export type EventStatus = "SCHEDULED" | "COMPLETED" | "CANCELLED" | "MISSED";

export interface AppointmentRequest {
  eventType: EventType;
  title: string;
  scheduledAt: IsoDateTime;
  durationMinutes: number | null;
  locationName: string | null;
  providerName: string | null;
  notes: string | null;
}

export interface Appointment extends AppointmentRequest {
  id: string;
  patientId: string;
  endsAt: IsoDateTime | null;
  status: EventStatus;
  summary: string | null;
  record: RecordInfo;
}

// agenda consolidada (WF03).
export type AgendaView = "DAY" | "WEEK" | "MONTH";
export type AgendaItemKind = "EVENT" | "MEDICATION" | "CARE_TASK";

export interface AgendaItem {
  kind: AgendaItemKind;
  sourceId: string;
  patientId: string;
  patientName: string;
  title: string;
  subtitle: string | null;
  startsAt: IsoDateTime;
  endsAt: IsoDateTime | null;
  done: boolean;
  overdue: boolean;
  eventStatus: EventStatus | null;
  eventType: EventType | null;
  locationName: string | null;
  providerName: string | null;
  administrationId: string | null;
  administrationStatus: AdministrationStatus | null;
  taskDate: IsoDate | null;
  completionId: string | null;
}

export interface Agenda {
  from: IsoDate;
  to: IsoDate;
  view: AgendaView;
  items: AgendaItem[];
}
