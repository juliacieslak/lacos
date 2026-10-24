// medições de saúde (RF09) e limites/alertas (RF13).
import { api } from "./api";
import { createRecordService } from "./recordService";
import type { HealthMeasurement, HealthThreshold, HealthThresholdRequest, MeasurementRequest } from "../types/healthMeasurement";

export const measurementService = {
  ...createRecordService<HealthMeasurement, MeasurementRequest>("measurements"),
  thresholds: (patientId: string, signal?: AbortSignal) =>
    api.get<HealthThreshold>(`/api/patients/${patientId}/health-thresholds`, undefined, signal),
  saveThresholds: (patientId: string, data: Partial<HealthThresholdRequest>) =>
    api.put<HealthThreshold>(`/api/patients/${patientId}/health-thresholds`, data),
};
