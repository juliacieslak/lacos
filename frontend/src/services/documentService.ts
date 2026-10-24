// documentos anexados (RF19 / US03). O conteúdo é baixado com o token e exibido via URL temporária (blob).
import { api, apiRequest } from "./api";
import type { PagedResult } from "../types/common";
import type { DocumentCategory, PatientDocument } from "../types/document";

const base = (patientId: string) => `/api/patients/${patientId}/documents`;

export const MAX_DOCUMENT_BYTES = 10 * 1024 * 1024;
export const ACCEPTED_DOCUMENT_TYPES = ["application/pdf", "image/jpeg", "image/png"];

export const documentService = {
  list: (patientId: string, category?: DocumentCategory, page = 1, signal?: AbortSignal) =>
    api.get<PagedResult<PatientDocument>>(base(patientId), { category, page, pageSize: 20 }, signal),
  upload: (patientId: string, file: File, category: DocumentCategory, description?: string) => {
    const form = new FormData();
    form.append("file", file);
    form.append("category", category);
    if (description) form.append("description", description);
    return apiRequest<PatientDocument>(base(patientId), { method: "POST", body: form, timeoutMs: 60_000 });
  },
  content: (patientId: string, documentId: string) =>
    apiRequest<Blob>(`${base(patientId)}/${documentId}/content`, { asBlob: true, timeoutMs: 60_000 }),
  remove: (patientId: string, documentId: string) => api.delete(`${base(patientId)}/${documentId}`),
};
