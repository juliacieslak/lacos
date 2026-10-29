// rótulos em português para os valores (enums) retornados pela API.
import type { CareCategory } from "../types/careTask";
import type { AllergySeverity, HealthConditionStatus, SymptomType } from "../types/clinical";
import type { DayOfWeek, Ethnicity, Gender } from "../types/common";
import type { DocumentCategory } from "../types/document";
import type { EventStatus, EventType } from "../types/appointment";
import type { HealthMetric } from "../types/healthMeasurement";
import type { TimelineCategory } from "../types/history";
import type { AdministrationRoute } from "../types/medication";
import type { NotificationCategory } from "../types/notification";
import type { PatientCareStatus } from "../types/patient";
import type { AccessProfilePreset, CareRole, InvitationStatus } from "../types/patientMember";
import type { ProfileType } from "../types/user";

export const profileTypeLabels: Record<ProfileType, string> = {
  FAMILY: "Familiar / responsável",
  CAREGIVER: "Cuidador(a)",
  HEALTH_PROFESSIONAL: "Profissional da saúde",
};

export const genderLabels: Record<Gender, string> = {
  FEMALE: "Feminino",
  MALE: "Masculino",
  NON_BINARY: "Não binário",
  OTHER: "Outro",
  PREFER_NOT_TO_SAY: "Prefiro não informar",
};

export const ethnicityLabels: Record<Ethnicity, string> = {
  BRANCA: "Branca",
  PRETA: "Preta",
  PARDA: "Parda",
  AMARELA: "Amarela",
  INDIGENA: "Indígena",
  NAO_DECLARADA: "Prefiro não declarar",
};

export const careStatusLabels: Record<PatientCareStatus, string> = {
  IN_FOLLOW_UP: "Em acompanhamento",
  PAUSED: "Pausado",
  DISCHARGED: "Encerrado",
};

export const roleLabels: Record<CareRole, string> = {
  ADMINISTRATOR: "Administrador",
  CAREGIVER: "Cuidador(a)",
  FAMILY_MEMBER: "Familiar",
  HEALTH_PROFESSIONAL: "Profissional da saúde",
};

export const presetLabels: Record<AccessProfilePreset, string> = {
  READ_ONLY: "Apenas leitura",
  EDITOR: "Edição",
  MANAGER: "Administração",
  CUSTOM: "Personalizado",
};

export const invitationStatusLabels: Record<InvitationStatus, string> = {
  PENDING: "Pendente",
  ACCEPTED: "Aceito",
  DECLINED: "Recusado",
  REVOKED: "Cancelado",
  EXPIRED: "Expirado",
};

export const routeLabels: Record<AdministrationRoute, string> = {
  ORAL: "Oral", SUBLINGUAL: "Sublingual", TOPICAL: "Tópica", TRANSDERMAL: "Transdérmica", INHALATION: "Inalatória",
  NASAL: "Nasal", OPHTHALMIC: "Oftálmica", OTIC: "Otológica", RECTAL: "Retal", VAGINAL: "Vaginal",
  SUBCUTANEOUS: "Subcutânea", INTRAMUSCULAR: "Intramuscular", INTRAVENOUS: "Intravenosa", OTHER: "Outra",
};

export const careCategoryLabels: Record<CareCategory, string> = {
  ACTIVITY: "Atividade", FOOD: "Alimentação", HYGIENE: "Higiene", SLEEP: "Sono", MEDICATION: "Medicação",
  CARE_PROVIDED: "Cuidado prestado", COMPLAINT: "Queixa", PAIN: "Dor", OTHER: "Outro",
};

export const eventTypeLabels: Record<EventType, string> = {
  APPOINTMENT: "Consulta", EXAM: "Exame", TREATMENT: "Tratamento", OTHER: "Outro",
};

export const eventStatusLabels: Record<EventStatus, string> = {
  SCHEDULED: "Agendado", COMPLETED: "Realizado", CANCELLED: "Cancelado", MISSED: "Não compareceu",
};

export const symptomTypeLabels: Record<SymptomType, string> = {
  SYMPTOM: "Sintoma", COMPLAINT: "Queixa", HEALTH_CHANGE: "Alteração no estado de saúde",
};

export const conditionStatusLabels: Record<HealthConditionStatus, string> = {
  ACTIVE: "Ativa", IN_REMISSION: "Em remissão", INACTIVE: "Inativa",
};

export const severityLabels: Record<AllergySeverity, string> = {
  MILD: "Leve", MODERATE: "Moderada", SEVERE: "Grave", LIFE_THREATENING: "Risco de vida",
};

export const documentCategoryLabels: Record<DocumentCategory, string> = {
  EXAM: "Exame", MEDICAL_GUIDE: "Guia médica", PRESCRIPTION: "Receita", OTHER: "Outro",
};

export const metricLabels: Record<HealthMetric, string> = {
  SYSTOLIC_PRESSURE: "Pressão sistólica", DIASTOLIC_PRESSURE: "Pressão diastólica", HEART_RATE: "Frequência cardíaca",
  TEMPERATURE: "Temperatura", RESPIRATORY_RATE: "Frequência respiratória", OXYGEN_SATURATION: "Saturação de oxigênio",
  BLOOD_GLUCOSE: "Glicemia",
};

export const timelineCategoryLabels: Record<TimelineCategory, string> = {
  MEDICATION: "Medicamentos", MEDICATION_ADMINISTRATION: "Doses", MEASUREMENT: "Medições", SYMPTOM: "Sintomas",
  CARE_DIARY: "Diário", EVENT: "Compromissos", DOCUMENT: "Documentos", HEALTH_CONDITION: "Condições",
  SURGERY: "Cirurgias", ALLERGY: "Alergias",
};

export const notificationCategoryLabels: Record<NotificationCategory, string> = {
  MEDICATION_REMINDER: "Lembretes de medicamentos",
  MEDICATION_MISSED: "Medicação não administrada",
  APPOINTMENT_REMINDER: "Lembretes de compromissos",
  CARE_ROUTINE_CHECKIN: "Rotina de cuidado e aferições",
  PATIENT_DATA_ADDED: "Novas informações registradas",
  HEALTH_THRESHOLD_ALERT: "Métricas fora do limite",
};

export const dayLabels: Record<DayOfWeek, string> = {
  SUNDAY: "Dom", MONDAY: "Seg", TUESDAY: "Ter", WEDNESDAY: "Qua", THURSDAY: "Qui", FRIDAY: "Sex", SATURDAY: "Sáb",
};

export const allDays: DayOfWeek[] = ["MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY"];

// converte um Record de rótulos em opções para <Select>.
export const toOptions = <T extends string>(labels: Record<T, string>) =>
  (Object.entries(labels) as [T, string][]).map(([value, label]) => ({ value, label }));
