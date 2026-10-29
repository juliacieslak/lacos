// formulário rápido para registrar pressão arterial (sistólica, diastólica, frequência cardíaca e observações).
import MeasurementForm from "./measurementForm";
import type { ComponentProps } from "react";

export default function BloodPressureForm(props: Omit<ComponentProps<typeof MeasurementForm>, "mode">) {
  return <MeasurementForm {...props} mode="pressure" />;
}
