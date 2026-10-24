import type { IsoDateTime, PagedResult } from "./common";

export type NotificationCategory =
  | "MEDICATION_REMINDER" | "MEDICATION_MISSED" | "APPOINTMENT_REMINDER" | "CARE_ROUTINE_CHECKIN"
  | "PATIENT_DATA_ADDED" | "HEALTH_THRESHOLD_ALERT";

export interface AppNotification {
  id: string;
  patientId: string | null;
  category: NotificationCategory;
  title: string;
  message: string;
  relatedEntityId: string | null;
  createdAt: IsoDateTime;
  readAt: IsoDateTime | null;
}

export interface NotificationList {
  page: PagedResult<AppNotification>;
  unreadCount: number;
}

export interface NotificationPreferenceRequest {
  medicationReminder: boolean;
  medicationMissed: boolean;
  appointmentReminder: boolean;
  careRoutineCheckin: boolean;
  patientDataAdded: boolean;
  healthThresholdAlert: boolean;
  isActive: boolean;
}

export interface NotificationPreference extends NotificationPreferenceRequest {
  patientId: string;
  patientName: string;
}
