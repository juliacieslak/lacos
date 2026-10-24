// limites mínimo/máximo das métricas e opção de alerta (RF13). Unidades canônicas: mmHg, bpm, °C, rpm, %, mg/dL.
import { useState, type FormEvent } from "react";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import { useAction } from "../../hooks/useAsync";
import type { HealthThreshold, HealthThresholdRequest, MetricLimit, ThresholdKey } from "../../types/healthMeasurement";

const metrics: { key: ThresholdKey; label: string; unit: string }[] = [
  { key: "systolicPressure", label: "Pressão sistólica", unit: "mmHg" },
  { key: "diastolicPressure", label: "Pressão diastólica", unit: "mmHg" },
  { key: "heartRate", label: "Frequência cardíaca", unit: "bpm" },
  { key: "bloodGlucose", label: "Glicemia", unit: "mg/dL" },
  { key: "temperature", label: "Temperatura", unit: "°C" },
  { key: "oxygenSaturation", label: "Saturação", unit: "%" },
  { key: "respiratoryRate", label: "Freq. respiratória", unit: "rpm" },
];

type Row = { min: string; minNotice: boolean; max: string; maxNotice: boolean };

export default function ThresholdsForm({ value, onSubmit, onCancel }: { value: HealthThreshold; onSubmit: (d: HealthThresholdRequest) => Promise<unknown>; onCancel: () => void }) {
  const [rows, setRows] = useState<Record<ThresholdKey, Row>>(() =>
    Object.fromEntries(metrics.map(({ key }) => {
      const l: MetricLimit = value[key];
      return [key, { min: l.min?.toString() ?? "", minNotice: l.minNotice, max: l.max?.toString() ?? "", maxNotice: l.maxNotice }];
    })) as Record<ThresholdKey, Row>);
  const save = useAction(onSubmit);
  const update = (key: ThresholdKey, patch: Partial<Row>) => setRows((r) => ({ ...r, [key]: { ...r[key], ...patch } }));

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const num = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));
    const data = Object.fromEntries(metrics.map(({ key }) => {
      const r = rows[key];
      return [key, { min: num(r.min), minNotice: r.minNotice && r.min !== "", max: num(r.max), maxNotice: r.maxNotice && r.max !== "" }];
    })) as HealthThresholdRequest;
    void save.run(data);
  };

  const errors = save.error ? Object.values(save.error.fieldErrors).flat() : [];
  return (
    <form onSubmit={handleSubmit}>
      <p className="mb-4 text-sm text-mist-500">Um alerta é enviado quando o valor registrado atingir ou ultrapassar o limite marcado.</p>
      <div className="space-y-3">
        {metrics.map(({ key, label, unit }) => (
          <fieldset key={key} className="grid grid-cols-2 items-end gap-3 rounded-xl border border-mist-200 p-3 sm:grid-cols-[1fr_120px_120px]">
            <legend className="sr-only">{label}</legend>
            <p className="col-span-2 text-sm font-semibold text-ink sm:col-span-1">{label} <span className="font-normal text-mist-400">({unit})</span></p>
            {(["min", "max"] as const).map((side) => (
              <div key={side}>
                <input aria-label={`${label} ${side === "min" ? "mínimo" : "máximo"}`} type="number" step="0.1" placeholder={side === "min" ? "Mínimo" : "Máximo"}
                  value={rows[key][side]} onChange={(e) => update(key, { [side]: e.target.value })}
                  className="w-full rounded-lg border border-mist-200 bg-mist-100 px-3 py-2 text-sm outline-none focus:border-lavender-500" />
                <label className="mt-1 flex items-center gap-1.5 text-xs text-mist-500">
                  <input type="checkbox" className="accent-lavender-600" checked={rows[key][side === "min" ? "minNotice" : "maxNotice"]}
                    onChange={(e) => update(key, side === "min" ? { minNotice: e.target.checked } : { maxNotice: e.target.checked })} />
                  Alertar
                </label>
              </div>
            ))}
          </fieldset>
        ))}
      </div>
      <ErrorMessage error={errors.length ? errors.join(" ") : save.error} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} />
    </form>
  );
}
