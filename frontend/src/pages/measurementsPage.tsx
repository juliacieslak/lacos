// aba de medições: registro rápido de pressão e glicemia, medição completa, histórico e limites de alerta (RF09/RF13).
import { Activity, Droplet, HeartPulse, Plus, SlidersHorizontal } from "lucide-react";
import { useState, type ReactNode } from "react";
import BloodPressureForm from "../components/measurements/bloodPressureForm";
import GlucoseForm from "../components/measurements/glucoseForm";
import MeasurementForm from "../components/measurements/measurementForm";
import MeasurementHistory from "../components/measurements/measurementHistory";
import ThresholdsForm from "../components/measurements/thresholdsForm";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import { usePatientContext } from "../contexts/patientContext";
import { useToast } from "../contexts/toastContext";
import { useConfirmDelete } from "../hooks/useConfirmDelete";
import { useMeasurements, useThresholds } from "../hooks/useMeasurements";
import { measurementService } from "../services/measurementService";
import type { HealthMeasurement, MeasurementRequest } from "../types/healthMeasurement";
import { metricLabels } from "../utils/labels";

type FormKind = "pressure" | "glucose" | "full" | "thresholds" | null;

export default function MeasurementsPage() {
  const { patient } = usePatientContext();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [form, setForm] = useState<FormKind>(null);
  const history = useMeasurements(patient.id, page);
  const thresholds = useThresholds(patient.id);
  const p = patient.myPermissions;
  const del = useConfirmDelete((m: HealthMeasurement) => measurementService.remove(patient.id, m.id), history.reload, "esta medição");

  const create = async (data: MeasurementRequest) => {
    const saved = await measurementService.create(patient.id, data);
    setForm(null);
    setPage(1);
    history.reload();
    if (saved.alerts.length) toast.error(`Atenção: ${saved.alerts.map((a) => metricLabels[a.metric]).join(", ")} fora do limite. A rede de cuidado foi notificada.`);
    else toast.success("Medição registrada.");
  };

  const formProps = { patient, onSubmit: create, onCancel: () => setForm(null) };

  return (
    <div className="space-y-6">
      {p.canAdd && (
        <div className="grid gap-3 sm:grid-cols-3">
          <QuickButton icon={<HeartPulse className="h-5 w-5" />} label="Registrar pressão" onClick={() => setForm("pressure")} />
          <QuickButton icon={<Droplet className="h-5 w-5" />} label="Registrar glicemia" onClick={() => setForm("glucose")} />
          <QuickButton icon={<Plus className="h-5 w-5" />} label="Outras medições" onClick={() => setForm("full")} />
        </div>
      )}

      <Card>
        <CardHeader title="Histórico de medições" icon={<Activity className="h-4 w-4" />}
          action={thresholds.data && (p.canAdd || p.canEdit) && (
            <Button size="sm" variant="secondary" icon={<SlidersHorizontal className="h-3.5 w-3.5" />} onClick={() => setForm("thresholds")}>Limites e alertas</Button>
          )} />
        {history.loading && !history.data ? <Loading /> : history.error ? <ErrorMessage error={history.error} onRetry={history.reload} /> : history.data && (
          <MeasurementHistory data={history.data} onPageChange={setPage} onDelete={p.canDelete ? del.request : undefined} />
        )}
      </Card>

      <Modal open={form === "pressure"} title="Registrar pressão arterial" onClose={() => setForm(null)}><BloodPressureForm {...formProps} /></Modal>
      <Modal open={form === "glucose"} title="Registrar glicemia" onClose={() => setForm(null)}><GlucoseForm {...formProps} /></Modal>
      <Modal open={form === "full"} title="Registrar medições" size="lg" onClose={() => setForm(null)}><MeasurementForm mode="full" {...formProps} /></Modal>
      <Modal open={form === "thresholds"} title="Limites e alertas" size="lg" onClose={() => setForm(null)}>
        {thresholds.data && (
          <ThresholdsForm value={thresholds.data} onCancel={() => setForm(null)} onSubmit={async (data) => {
            thresholds.setData(await measurementService.saveThresholds(patient.id, data));
            toast.success("Limites atualizados.");
            setForm(null);
          }} />
        )}
      </Modal>
      {del.element}
    </div>
  );
}

function QuickButton({ icon, label, onClick }: { icon: ReactNode; label: string; onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="flex items-center gap-3 rounded-2xl border border-mist-200 bg-white p-4 text-left text-sm font-semibold text-ink shadow-[var(--shadow-card)] transition hover:border-lavender-300 hover:bg-lavender-50">
      <span className="grid h-10 w-10 place-items-center rounded-xl bg-lavender-100 text-lavender-600">{icon}</span>
      {label}
    </button>
  );
}
