// fábrica de serviços CRUD para registros clínicos do paciente.
// o backend expõe o mesmo contrato para todos: GET (paginado), GET {id}, POST, PUT {id}, DELETE {id}.
import { api } from "./api";
import type { PagedResult } from "../types/common";

export interface ListParams {
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export function createRecordService<TResponse, TRequest>(resource: string) {
  const url = (patientId: string, id?: string) => `/api/patients/${patientId}/${resource}${id ? `/${id}` : ""}`;
  return {
    list: (patientId: string, params: ListParams = {}, signal?: AbortSignal) =>
      api.get<PagedResult<TResponse>>(url(patientId), { ...params }, signal),
    get: (patientId: string, id: string) => api.get<TResponse>(url(patientId, id)),
    create: (patientId: string, data: TRequest) => api.post<TResponse>(url(patientId), data),
    update: (patientId: string, id: string, data: TRequest) => api.put<TResponse>(url(patientId, id), data),
    remove: (patientId: string, id: string) => api.delete(url(patientId, id)),
  };
}
