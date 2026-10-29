// controla medições, histórico paginado e limites clínicos.
import { measurementService } from "../services/measurementService";
import { useAsync } from "./useAsync";

export function useMeasurements(patientId: string, page: number) {
  return useAsync((signal) => measurementService.list(patientId, { page, pageSize: 10 }, signal), [patientId, page]);
}

export function useThresholds(patientId: string) {
  return useAsync((signal) => measurementService.thresholds(patientId, signal), [patientId]);
}
