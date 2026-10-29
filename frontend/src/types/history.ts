import type { IsoDateTime } from "./common";

export type TimelineCategory =
  | "MEDICATION" | "MEDICATION_ADMINISTRATION" | "MEASUREMENT" | "SYMPTOM" | "CARE_DIARY" | "EVENT"
  | "DOCUMENT" | "HEALTH_CONDITION" | "SURGERY" | "ALLERGY";

export interface TimelineItem {
  category: TimelineCategory;
  entityId: string;
  occurredAt: IsoDateTime;
  title: string;
  description: string | null;
  recordedByUserId: string;
  recordedByName: string | null;
  details: Record<string, unknown>;
}

export type AuditAction = "CREATE" | "UPDATE" | "DELETE" | "STATUS_CHANGE" | "PROFILE_COMPLETED" | "VIEW" | "ERASURE";

export interface AuditLogEntry {
  id: string;
  userId: string | null;
  userName: string | null;
  entityType: string;
  entityId: string | null;
  action: AuditAction;
  oldValues: Record<string, unknown> | null;
  newValues: Record<string, unknown> | null;
  reason: string | null;
  ipAddress: string | null;
  createdAt: IsoDateTime;
}
