// tipos compartilhados — espelham os contratos JSON do backend (enums em UPPER_SNAKE_CASE).

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface Permissions {
  canView: boolean;
  canAdd: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canManageMembers: boolean;
}

// metadados de rastreabilidade presentes em todo registro clínico (quem registrou e quando).
export interface RecordInfo {
  createdByUserId: string;
  createdByName: string | null;
  updatedByUserId: string | null;
  updatedByName: string | null;
  createdAt: string;
  updatedAt: string;
}

export type Gender = "FEMALE" | "MALE" | "NON_BINARY" | "OTHER" | "PREFER_NOT_TO_SAY";
export type Ethnicity = "BRANCA" | "PRETA" | "PARDA" | "AMARELA" | "INDIGENA" | "NAO_DECLARADA";
export type DayOfWeek = "SUNDAY" | "MONDAY" | "TUESDAY" | "WEDNESDAY" | "THURSDAY" | "FRIDAY" | "SATURDAY";

// datas no formato ISO: DateOnly = "yyyy-MM-dd", TimeOnly = "HH:mm:ss", instantes = ISO 8601.
export type IsoDate = string;
export type IsoTime = string;
export type IsoDateTime = string;
