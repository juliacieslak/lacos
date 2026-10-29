// formulário de medição. Os modos "pressure" e "glucose" são os atalhos rápidos pedidos pelo cuidador
// (validação da Sprint 3); o modo "full" permite registrar todas as métricas do RF09.
import { useState, type FormEvent } from "react";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import Input, { Textarea } from "../ui/input";
import { useAction } from "../../hooks/useAsync";
import type { MeasurementRequest } from "../../types/healthMeasurement";
import type { Patient } from "../../types/patient";
import { fromDateTimeLocal, toDateTimeLocal } from "../../utils/formatDate";

export type MeasurementMode = "pressure" | "glucose" | "full";

type Values = Record<"systolic" | "diastolic" | "heartRate" | "bloodGlucose" | "temperature" | "oxygenSaturation" | "respiratoryRate" | "weight" | "painLevel", string>;

interface Props {
  patient: Patient;
  mode: MeasurementMode;
  onSubmit: (data: MeasurementRequest) => Promise<unknown>;
  onCancel: () => void;
}

const empty: Values = { systolic: "", diastolic: "", heartRate: "", bloodGlucose: "", temperature: "", oxygenSaturation: "", respiratoryRate: "", weight: "", painLevel: "" };

export default function MeasurementForm({ patient, mode, onSubmit, onCancel }: Props) {
  const [values, setValues] = useState<Values>(empty);
  const [measuredAt, setMeasuredAt] = useState(toDateTimeLocal());
  const [description, setDescription] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);
  const save = useAction(onSubmit);
  const set = (k: keyof Values, v: string) => setValues((s) => ({ ...s, [k]: v }));
  const n = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));
  const err = (k: string) => save.error?.fieldErrors[k]?.[0];
  const glucoseUnit = patient.units.glucose === "MG_PER_DL" ? "mg/dL" : "mmol/L";
  const tempUnit = patient.units.temperature === "CELSIUS" ? "°C" : "°F";
  const weightUnit = patient.units.weight === "KILOGRAM" ? "kg" : "lb";

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const data: MeasurementRequest = {
      measuredAt: fromDateTimeLocal(measuredAt),
      systolic: n(values.systolic), diastolic: n(values.diastolic), heartRate: n(values.heartRate),
      bloodGlucose: n(values.bloodGlucose), temperature: n(values.temperature), oxygenSaturation: n(values.oxygenSaturation),
      respiratoryRate: n(values.respiratoryRate), weight: n(values.weight), painLevel: n(values.painLevel),
      description: description.trim() || null,
      // unidades nulas = unidades configuradas no paciente (RF09).
      temperatureUnit: null, glucoseUnit: null, weightUnit: null,
    };
    if ((data.systolic === null) !== (data.diastolic === null)) return setLocalError("Informe a pressão sistólica e a diastólica.");
    if (Object.entries(data).every(([k, v]) => ["measuredAt", "description", "temperatureUnit", "glucoseUnit", "weightUnit"].includes(k) || v === null))
      return setLocalError("Informe ao menos uma medição.");
    setLocalError(null);
    await save.run(data);
  };

  const showPressure = mode !== "glucose";
  const showGlucose = mode !== "pressure";

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        {showPressure && (
          <>
            <Input label="Sistólica (mmHg)" type="number" inputMode="numeric" value={values.systolic} onChange={(e) => set("systolic", e.target.value)} error={err("systolic")} placeholder="120" />
            <Input label="Diastólica (mmHg)" type="number" inputMode="numeric" value={values.diastolic} onChange={(e) => set("diastolic", e.target.value)} error={err("diastolic")} placeholder="80" />
            <Input label="Frequência cardíaca (bpm)" type="number" inputMode="numeric" value={values.heartRate} onChange={(e) => set("heartRate", e.target.value)} error={err("heartRate")} />
          </>
        )}
        {showGlucose && (
          <Input label={`Glicemia (${glucoseUnit})`} type="number" inputMode="decimal" step="0.1" value={values.bloodGlucose} onChange={(e) => set("bloodGlucose", e.target.value)} error={err("bloodGlucose")} />
        )}
        {mode === "full" && (
          <>
            <Input label={`Temperatura (${tempUnit})`} type="number" inputMode="decimal" step="0.1" value={values.temperature} onChange={(e) => set("temperature", e.target.value)} error={err("temperature")} />
            <Input label="Saturação de O₂ (%)" type="number" inputMode="numeric" value={values.oxygenSaturation} onChange={(e) => set("oxygenSaturation", e.target.value)} error={err("oxygenSaturation")} />
            <Input label="Freq. respiratória (rpm)" type="number" inputMode="numeric" value={values.respiratoryRate} onChange={(e) => set("respiratoryRate", e.target.value)} error={err("respiratoryRate")} />
            <Input label={`Peso (${weightUnit})`} type="number" inputMode="decimal" step="0.1" value={values.weight} onChange={(e) => set("weight", e.target.value)} error={err("weight")} />
            <Input label="Nível de dor (0 a 10)" type="number" min={0} max={10} value={values.painLevel} onChange={(e) => set("painLevel", e.target.value)} error={err("painLevel")} />
          </>
        )}
        <Input label="Data e hora" type="datetime-local" value={measuredAt} onChange={(e) => setMeasuredAt(e.target.value)} error={err("measuredAt")} />
        <Textarea label={mode === "glucose" ? "Contexto (jejum, após refeição...)" : "Observações"} value={description} maxLength={2000}
          onChange={(e) => setDescription(e.target.value)} containerClassName="sm:col-span-2" />
      </div>
      <ErrorMessage error={localError ?? (save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null)} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} submitLabel="Registrar" />
    </form>
  );
}
