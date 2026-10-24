// exibe uma medição: valores, alertas de limite, data e responsável.
import { AlertTriangle, Trash2 } from "lucide-react";
import Badge from "../ui/badge";
import RecordMeta from "../ui/recordMeta";
import type { HealthMeasurement } from "../../types/healthMeasurement";
import { formatDateTime } from "../../utils/formatDate";
import { formatMeasurementParts } from "../../utils/formatMeasurement";
import { metricLabels } from "../../utils/labels";

export default function MeasurementCard({ measurement, onDelete }: { measurement: HealthMeasurement; onDelete?: () => void }) {
  return (
    <li className="rounded-2xl border border-mist-200 bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-semibold text-ink">{formatDateTime(measurement.measuredAt)}</p>
        {onDelete && (
          <button type="button" onClick={onDelete} className="rounded-lg p-1.5 text-mist-400 hover:bg-danger-50 hover:text-danger-600" aria-label="Excluir medição">
            <Trash2 className="h-4 w-4" />
          </button>
        )}
      </div>
      <dl className="mt-2 flex flex-wrap gap-x-6 gap-y-2">
        {formatMeasurementParts(measurement).map((part) => (
          <div key={part.label}>
            <dt className="text-[11px] uppercase tracking-[0.06em] text-mist-400">{part.label}</dt>
            <dd className="font-display font-semibold text-ink">{part.value}</dd>
          </div>
        ))}
      </dl>
      {measurement.alerts.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1.5">
          {measurement.alerts.map((a) => (
            <Badge key={a.metric} tone="danger"><AlertTriangle className="h-3 w-3" /> {metricLabels[a.metric]} {a.isAboveMax ? "acima" : "abaixo"} do limite ({a.limit})</Badge>
          ))}
        </div>
      )}
      {measurement.description && <p className="mt-2 text-sm text-mist-700">{measurement.description}</p>}
      <div className="mt-2"><RecordMeta record={measurement.record} /></div>
    </li>
  );
}
