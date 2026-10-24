// histórico clínico e acompanhamento: sintomas, diário, condições (CID), cirurgias e alergias.
import { createRecordService } from "./recordService";
import type {
  Allergy, AllergyRequest, DiaryEntry, DiaryRequest, HealthCondition, HealthConditionRequest,
  Surgery, SurgeryRequest, Symptom, SymptomRequest,
} from "../types/clinical";

export const symptomService = createRecordService<Symptom, SymptomRequest>("symptoms");
export const diaryService = createRecordService<DiaryEntry, DiaryRequest>("care-diary");
export const conditionService = createRecordService<HealthCondition, HealthConditionRequest>("health-conditions");
export const surgeryService = createRecordService<Surgery, SurgeryRequest>("surgeries");
export const allergyService = createRecordService<Allergy, AllergyRequest>("allergies");
