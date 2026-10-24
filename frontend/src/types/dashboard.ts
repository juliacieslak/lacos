import type { IsoDate, IsoDateTime } from "./common";
import type { AgendaItem } from "./appointment";
import type { HealthMetric } from "./healthMeasurement";
import type { PatientSummary } from "./patient";
import type { CareTeamMember } from "./patientMember";

export interface LatestMetric {
  metric: HealthMetric | null;
  name: string;
  value: string;
  measuredAt: IsoDateTime;
}

export interface Dashboard {
  date: IsoDate;
  userName: string;
  patients: PatientSummary[];
  selectedPatientId: string | null;
  todayAgenda: AgendaItem[];
  todayPending: number;
  todayCompleted: number;
  latestMetrics: LatestMetric[];
  careTeam: CareTeamMember[];
  unreadNotifications: number;
}
