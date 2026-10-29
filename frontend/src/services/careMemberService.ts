// rede de cuidado: membros, permissões e convites (US01, UC04, UC05).
import { api } from "./api";
import type { CareTeamMember, Invitation, InviteMemberRequest, MemberRequest } from "../types/patientMember";

const base = (patientId: string) => `/api/patients/${patientId}`;

export const careMemberService = {
  members: (patientId: string, includeInactive = false, signal?: AbortSignal) =>
    api.get<CareTeamMember[]>(`${base(patientId)}/members`, { includeInactive }, signal),
  updateMember: (patientId: string, permissionId: string, data: MemberRequest) =>
    api.put<CareTeamMember>(`${base(patientId)}/members/${permissionId}`, data),
  revokeMember: (patientId: string, permissionId: string, reason?: string) =>
    api.delete(`${base(patientId)}/members/${permissionId}`, { reason: reason ?? null }),
  invitations: (patientId: string, signal?: AbortSignal) => api.get<Invitation[]>(`${base(patientId)}/invitations`, undefined, signal),
  invite: (patientId: string, data: InviteMemberRequest) => api.post<Invitation>(`${base(patientId)}/invitations`, data),
  revokeInvitation: (patientId: string, invitationId: string) => api.delete(`${base(patientId)}/invitations/${invitationId}`),
  myInvitations: (signal?: AbortSignal) => api.get<Invitation[]>("/api/invitations/mine", undefined, signal),
  accept: (invitationId: string) => api.post<CareTeamMember>(`/api/invitations/${invitationId}/accept`),
  decline: (invitationId: string) => api.post<void>(`/api/invitations/${invitationId}/decline`),
};
