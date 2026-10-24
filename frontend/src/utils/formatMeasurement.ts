// formata valores de pressão, glicemia, temperatura e peso conforme as unidades informadas.
import type { HealthMeasurement } from "../types/healthMeasurement";

const num = (v: number, digits = 1) => v.toLocaleString("pt-BR", { maximumFractionDigits: digits });

export function formatMeasurementParts(m: HealthMeasurement) {
  const parts: { label: string; value: string }[] = [];
  if (m.systolic != null && m.diastolic != null) parts.push({ label: "Pressão", value: `${m.systolic}/${m.diastolic} mmHg` });
  if (m.heartRate != null) parts.push({ label: "Freq. cardíaca", value: `${m.heartRate} bpm` });
  if (m.bloodGlucose != null)
    parts.push({ label: "Glicemia", value: `${num(m.bloodGlucose, 2)} ${m.glucoseUnit === "MG_PER_DL" ? "mg/dL" : "mmol/L"}` });
  if (m.temperature != null) parts.push({ label: "Temperatura", value: `${num(m.temperature)} ${m.temperatureUnit === "CELSIUS" ? "°C" : "°F"}` });
  if (m.oxygenSaturation != null) parts.push({ label: "Saturação", value: `${m.oxygenSaturation}%` });
  if (m.respiratoryRate != null) parts.push({ label: "Freq. respiratória", value: `${m.respiratoryRate} rpm` });
  if (m.weight != null) parts.push({ label: "Peso", value: `${num(m.weight, 2)} ${m.weightUnit === "KILOGRAM" ? "kg" : "lb"}` });
  if (m.painLevel != null) parts.push({ label: "Dor", value: `${m.painLevel}/10` });
  return parts;
}

export function formatDistance(meters: number) {
  return meters < 1000 ? `${meters} m` : `${(meters / 1000).toLocaleString("pt-BR", { maximumFractionDigits: 1 })} km`;
}

export function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toLocaleString("pt-BR", { maximumFractionDigits: 1 })} MB`;
}
