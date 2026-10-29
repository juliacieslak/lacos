import type { IsoDateTime, RecordInfo } from "./common";

export type TemperatureUnit = "CELSIUS" | "FAHRENHEIT";
export type GlucoseUnit = "MG_PER_DL" | "MMOL_PER_L";
export type WeightUnit = "KILOGRAM" | "POUND";
export type HealthMetric =
  | "SYSTOLIC_PRESSURE"
  | "DIASTOLIC_PRESSURE"
  | "HEART_RATE"
  | "TEMPERATURE"
  | "RESPIRATORY_RATE"
  | "OXYGEN_SATURATION"
  | "BLOOD_GLUCOSE";

export interface MeasurementValues {
  systolic: number | null;
  diastolic: number | null;
  heartRate: number | null;
  temperature: number | null;
  respiratoryRate: number | null;
  oxygenSaturation: number | null;
  bloodGlucose: number | null;
  painLevel: number | null;
  weight: number | null;
  description: string | null;
}

export interface MeasurementRequest extends MeasurementValues {
  measuredAt: IsoDateTime | null;
  temperatureUnit: TemperatureUnit | null;
  glucoseUnit: GlucoseUnit | null;
  weightUnit: WeightUnit | null;
}

export interface ThresholdAlert {
  metric: HealthMetric;
  value: number;
  limit: number;
  isAboveMax: boolean;
}

export interface HealthMeasurement extends MeasurementValues {
  id: string;
  patientId: string;
  measuredAt: IsoDateTime;
  temperatureUnit: TemperatureUnit;
  glucoseUnit: GlucoseUnit;
  weightUnit: WeightUnit;
  alerts: ThresholdAlert[];
  record: RecordInfo;
}

export interface MetricLimit {
  min: number | null;
  minNotice: boolean;
  max: number | null;
  maxNotice: boolean;
}

export type ThresholdKey =
  | "systolicPressure"
  | "diastolicPressure"
  | "heartRate"
  | "temperature"
  | "respiratoryRate"
  | "oxygenSaturation"
  | "bloodGlucose";

export type HealthThresholdRequest = Record<ThresholdKey, MetricLimit | null>;

export type HealthThreshold = Record<ThresholdKey, MetricLimit> & {
  id: string | null;
  patientId: string;
  updatedAt: IsoDateTime | null;
};
