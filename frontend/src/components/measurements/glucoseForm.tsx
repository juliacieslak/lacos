// formulário rápido para registrar glicemia (valor, unidade do paciente e contexto da medição).
import MeasurementForm from "./measurementForm";
import type { ComponentProps } from "react";

export default function GlucoseForm(props: Omit<ComponentProps<typeof MeasurementForm>, "mode">) {
  return <MeasurementForm {...props} mode="glucose" />;
}
