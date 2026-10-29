import type { IsoDateTime, Permissions } from "./common";
import type { ProfileType } from "./user";

export type CareRole = "ADMINISTRATOR" | "CAREGIVER" | "FAMILY_MEMBER" | "HEALTH_PROFESSIONAL";
export type AccessProfilePreset = "READ_ONLY" | "EDITOR" | "MANAGER" | "CUSTOM";
export type InvitationStatus = "PENDING" | "ACCEPTED" | "DECLINED" | "REVOKED" | "EXPIRED";

export interface CareTeamMember {
  permissionId: string;
  userId: string;
  userPublicCode: string;
  name: string;
  email: string;
  profileType: ProfileType | null;
  role: CareRole;
  relationshipType: string;
  permissions: Permissions;
  isActive: boolean;
  since: IsoDateTime;
  updatedAt: IsoDateTime;
}

export interface MemberRequest {
  role: CareRole;
  relationshipType: string;
  preset: AccessProfilePreset;
  permissions: Permissions | null;
}

export interface InviteMemberRequest extends MemberRequest {
  email: string;
}

export interface Invitation {
  id: string;
  patientId: string;
  patientName: string;
  invitedEmail: string;
  role: CareRole;
  relationshipType: string;
  permissions: Permissions;
  status: InvitationStatus;
  expiresAt: IsoDateTime;
  invitedByName: string | null;
  createdAt: IsoDateTime;
}
