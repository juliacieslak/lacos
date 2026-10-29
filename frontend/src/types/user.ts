import type { Ethnicity, Gender, IsoDate, IsoDateTime } from "./common";
import type { Invitation } from "./patientMember";

export type ProfileType = "FAMILY" | "CAREGIVER" | "HEALTH_PROFESSIONAL";
export type AccessStatus = "AUTHORIZED" | "PENDING_APPROVAL" | "NO_INVITATION";
export type ConsentType = "TERMS_OF_USE" | "SENSITIVE_HEALTH_DATA";

export interface User {
  id: string;
  publicCode: string;
  name: string;
  email: string;
  phone: string | null;
  profileType: ProfileType | null;
  profileCompleted: boolean;
  isActive: boolean;
  cpfMasked: string | null;
  birthDate: IsoDate | null;
  profession: string | null;
  cep: string | null;
  ethnicity: Ethnicity | null;
  gender: Gender | null;
  createdAt: IsoDateTime;
}

// resposta de POST /api/auth/session.
export interface Session {
  user: User;
  accessStatus: AccessStatus;
  consentsUpToDate: boolean;
  pendingInvitations: Invitation[];
  sessionTimeoutMinutes: number;
}

export interface ProfileFields {
  profileType: ProfileType;
  name: string;
  birthDate: IsoDate;
  phone: string;
  profession: string;
  cep: string;
  ethnicity: Ethnicity;
  gender: Gender;
}

export interface CompleteOnboardingRequest extends ProfileFields {
  cpf: string;
  acceptTermsOfUse: boolean;
  acceptSensitiveDataProcessing: boolean;
}

export interface Consent {
  consentType: ConsentType;
  version: string;
  acceptedAt: IsoDateTime;
  revokedAt: IsoDateTime | null;
  isCurrent: boolean;
}

export interface ConsentStatus {
  termsOfUseVersion: string;
  sensitiveDataConsentVersion: string;
  upToDate: boolean;
  consents: Consent[];
}
