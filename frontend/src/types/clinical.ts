import type { IsoDate, IsoDateTime, RecordInfo } from "./common";
import type { CareCategory } from "./careTask";

export type SymptomType = "SYMPTOM" | "COMPLAINT" | "HEALTH_CHANGE";
export type HealthConditionStatus = "ACTIVE" | "IN_REMISSION" | "INACTIVE";
export type AllergySeverity = "MILD" | "MODERATE" | "SEVERE" | "LIFE_THREATENING";

export interface SymptomRequest { type: SymptomType; description: string; occurredAt: IsoDateTime | null }
export interface Symptom extends SymptomRequest { id: string; patientId: string; occurredAt: IsoDateTime; record: RecordInfo }

export interface DiaryRequest { category: CareCategory; description: string; occurredAt: IsoDateTime | null }
export interface DiaryEntry extends DiaryRequest { id: string; patientId: string; occurredAt: IsoDateTime; record: RecordInfo }

export interface HealthConditionRequest {
  name: string; cidCode: string; diagnosisDate: IsoDate | null; status: HealthConditionStatus; notes: string | null;
}
export interface HealthCondition extends HealthConditionRequest { id: string; patientId: string; record: RecordInfo }

export interface SurgeryRequest { procedureName: string; performedOn: IsoDate; reason: string }
export interface Surgery extends SurgeryRequest { id: string; patientId: string; record: RecordInfo }

export interface AllergyRequest {
  causativeAgent: string; manifestation: string; identifiedOn: IsoDate | null; severity: AllergySeverity;
}
export interface Allergy extends AllergyRequest { id: string; patientId: string; record: RecordInfo }
