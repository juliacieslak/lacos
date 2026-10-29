import type { Ethnicity, Gender, IsoDate, IsoDateTime, Permissions } from "./common";
import type { CareRole } from "./patientMember";
import type { GlucoseUnit, TemperatureUnit, WeightUnit } from "./healthMeasurement";

export type PatientCareStatus = "IN_FOLLOW_UP" | "PAUSED" | "DISCHARGED";

export interface Patient {
  id: string;
  publicCode: string;
  fullName: string;
  birthDate: IsoDate;
  age: number;
  birthPlace: string;
  cep: string;
  ethnicity: Ethnicity;
  cpfMasked: string | null;
  gender: Gender;
  email: string | null;
  phone: string | null;
  heightCm: number;
  careStatus: PatientCareStatus;
  units: { temperature: TemperatureUnit; glucose: GlucoseUnit; weight: WeightUnit };
  myRole: CareRole;
  myRelationshipType: string;
  myPermissions: Permissions;
  createdAt: IsoDateTime;
  updatedAt: IsoDateTime;
}

// item da lista "Meus pacientes" (WF02).
export interface PatientSummary {
  id: string;
  publicCode: string;
  fullName: string;
  age: number;
  careStatus: PatientCareStatus;
  activeConditions: string[];
  todayCareItems: number;
  todayPendingCareItems: number;
  nextAppointmentAt: IsoDateTime | null;
  nextAppointmentTitle: string | null;
  myRole: CareRole;
  myPermissions: Permissions;
}

export interface UpdatePatientRequest {
  fullName: string;
  birthDate: IsoDate;
  birthPlace: string;
  cep: string;
  ethnicity: Ethnicity;
  gender: Gender;
  email: string | null;
  phone: string | null;
  heightCm: number;
}

export interface CreatePatientRequest extends UpdatePatientRequest {
  cpf: string;
  relationshipType: string;
}
