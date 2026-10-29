import type { DayOfWeek, IsoDate, IsoDateTime, IsoTime, RecordInfo } from "./common";

export type CareCategory =
  | "ACTIVITY" | "FOOD" | "HYGIENE" | "SLEEP" | "MEDICATION" | "CARE_PROVIDED" | "COMPLAINT" | "PAIN" | "OTHER";

// tarefa recorrente da rotina de cuidado.
export interface CareTaskRequest {
  title: string;
  description: string | null;
  category: CareCategory;
  scheduledTime: IsoTime;
  daysOfWeek: DayOfWeek[];
  startDate: IsoDate | null;
  endDate: IsoDate | null;
}

export interface CareTask extends Omit<CareTaskRequest, "startDate"> {
  id: string;
  patientId: string;
  startDate: IsoDate;
  record: RecordInfo;
}

export interface CareTaskCompletion {
  id: string;
  taskId: string;
  scheduledDate: IsoDate;
  completedAt: IsoDateTime;
  notes: string | null;
  completedByUserId: string;
}
