// linha do tempo (US04) e histórico de alterações (RF06).
import { api } from "./api";
import type { PagedResult } from "../types/common";
import type { AuditLogEntry, TimelineCategory, TimelineItem } from "../types/history";

export const historyService = {
  timeline: (patientId: string, categories: TimelineCategory[], page = 1, signal?: AbortSignal) =>
    api.get<PagedResult<TimelineItem>>(`/api/patients/${patientId}/timeline`, { categories, page, pageSize: 20 }, signal),
  changes: (patientId: string, page = 1, signal?: AbortSignal) =>
    api.get<PagedResult<AuditLogEntry>>(`/api/patients/${patientId}/history`, { page, pageSize: 20 }, signal),
};
