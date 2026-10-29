// cards com as últimas medições (pressão, glicemia...) do paciente selecionado.
import { Activity, Droplet, HeartPulse, Thermometer, Weight, Wind } from "lucide-react";
import type { LatestMetric } from "../../types/dashboard";
import { formatDateTime } from "../../utils/formatDate";

const icons = { SYSTOLIC_PRESSURE: HeartPulse, BLOOD_GLUCOSE: Droplet, HEART_RATE: Activity, TEMPERATURE: Thermometer, OXYGEN_SATURATION: Wind };

export default function HealthSummaryCards({ metrics }: { metrics: LatestMetric[] }) {
  if (metrics.length === 0)
    return <p className="rounded-2xl border border-dashed border-mist-300 bg-white/60 p-4 text-sm text-mist-500">Nenhuma medição registrada ainda.</p>;
  return (
    <div className="grid grid-cols-2 gap-3">
      {metrics.slice(0, 4).map((m) => {
        const Icon = (m.metric && icons[m.metric as keyof typeof icons]) || Weight;
        return (
          <div key={m.name} className="rounded-2xl border border-mist-200 bg-white p-4 shadow-[var(--shadow-card)]">
            <div className="flex items-center gap-2 text-xs font-medium text-mist-500"><Icon className="h-4 w-4 text-lavender-500" /> {m.name}</div>
            <p className="mt-2 font-display text-lg font-semibold text-ink">{m.value}</p>
            <p className="text-[11px] text-mist-400">{formatDateTime(m.measuredAt)}</p>
          </div>
        );
      })}
    </div>
  );
}
