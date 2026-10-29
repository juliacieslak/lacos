import type { RecordInfo } from "./common";

export type DocumentCategory = "EXAM" | "MEDICAL_GUIDE" | "PRESCRIPTION" | "OTHER";

export interface PatientDocument {
  id: string;
  patientId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  category: DocumentCategory;
  description: string | null;
  relatedEventId: string | null;
  record: RecordInfo;
}
