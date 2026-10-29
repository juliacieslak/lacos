import type { DayOfWeek, IsoDate, IsoDateTime, IsoTime, RecordInfo } from "./common";

export type AdministrationRoute =
  | "ORAL" | "SUBLINGUAL" | "TOPICAL" | "TRANSDERMAL" | "INHALATION" | "NASAL" | "OPHTHALMIC"
  | "OTIC" | "RECTAL" | "VAGINAL" | "SUBCUTANEOUS" | "INTRAMUSCULAR" | "INTRAVENOUS" | "OTHER";
export type AdministrationStatus = "ADMINISTERED" | "NOT_ADMINISTERED";

export interface MedicationRequest {
  name: string;
  dosage: string;
  brand: string | null;
  usageReason: string | null;
  administrationRoute: AdministrationRoute;
  prescribedBy: string | null;
  startDate: IsoDate;
  endDate: IsoDate | null;
  frequency: string;
  administrationTimes: IsoTime[];
  daysOfWeek: DayOfWeek[];
}

export interface Medication extends MedicationRequest {
  id: string;
  patientId: string;
  isActiveToday: boolean;
  record: RecordInfo;
}

export interface MedicationAdministrationRequest {
  medicationId: string;
  scheduledFor: IsoDateTime | null;
  administeredAt: IsoDateTime | null;
  status: AdministrationStatus;
  notes: string | null;
}

export interface MedicationAdministration {
  id: string;
  patientId: string;
  medicationId: string;
  medicationName: string | null;
  scheduledFor: IsoDateTime | null;
  administeredAt: IsoDateTime;
  status: AdministrationStatus;
  notes: string | null;
  record: RecordInfo;
}
