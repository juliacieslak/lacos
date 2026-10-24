// aba "Resumo": dados cadastrais, situação do acompanhamento, unidades de medida e exclusão definitiva (LGPD).
import { Pencil, Trash2 } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import PatientForm from "../components/patients/patientForm";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import FormActions from "../components/ui/formActions";
import Input, { SelectField } from "../components/ui/input";
import Modal from "../components/ui/modal";
import { usePatientContext } from "../contexts/patientContext";
import { useToast } from "../contexts/toastContext";
import { useAction } from "../hooks/useAsync";
import { patientService } from "../services/patientService";
import type { GlucoseUnit, TemperatureUnit, WeightUnit } from "../types/healthMeasurement";
import type { PatientCareStatus, UpdatePatientRequest } from "../types/patient";
import { formatDate } from "../utils/formatDate";
import { formatCep } from "../utils/formatCep";
import { careStatusLabels, ethnicityLabels, genderLabels, toOptions } from "../utils/labels";

export default function PatientOverviewPage() {
  const { patient, reloadPatient } = usePatientContext();
  const toast = useToast();
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [erasing, setErasing] = useState(false);
  const p = patient.myPermissions;

  const changeStatus = useAction(async (status: PatientCareStatus) => {
    await patientService.changeStatus(patient.id, status);
    toast.success("Situação do acompanhamento atualizada.");
    reloadPatient();
  });
  const changeUnits = useAction(async (t: TemperatureUnit, g: GlucoseUnit, w: WeightUnit) => {
    await patientService.updateUnits(patient.id, t, g, w);
    toast.success("Unidades de medida atualizadas.");
    reloadPatient();
  });

  const info: [string, string][] = [
    ["Data de nascimento", `${formatDate(patient.birthDate)} (${patient.age} anos)`],
    ["Local de nascimento", patient.birthPlace],
    ["CPF", patient.cpfMasked ?? "—"],
    ["CEP", formatCep(patient.cep)],
    ["Gênero", genderLabels[patient.gender]],
    ["Etnia", ethnicityLabels[patient.ethnicity]],
    ["Altura", `${patient.heightCm} cm`],
    ["Telefone", patient.phone ?? "—"],
    ["E-mail", patient.email ?? "—"],
    ["Sua relação", patient.myRelationshipType],
  ];

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
      <Card>
        <CardHeader title="Dados do paciente" action={p.canEdit && <Button size="sm" variant="secondary" icon={<Pencil className="h-3.5 w-3.5" />} onClick={() => setEditing(true)}>Editar</Button>} />
        <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
          {info.map(([label, value]) => (
            <div key={label}>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-mist-400">{label}</dt>
              <dd className="mt-0.5 text-sm text-ink">{value}</dd>
            </div>
          ))}
        </dl>
      </Card>

      <div className="space-y-6">
        <Card>
          <CardHeader title="Acompanhamento" />
          <SelectField label="Situação" options={toOptions(careStatusLabels)} value={patient.careStatus} disabled={!p.canEdit || changeStatus.pending}
            onChange={(e) => void changeStatus.run(e.target.value as PatientCareStatus)} hint="Lembretes automáticos são enviados apenas para pacientes em acompanhamento." />
          <ErrorMessage error={changeStatus.error} className="mt-3" />
        </Card>
        <Card>
          <CardHeader title="Unidades de medida" subtitle="Usadas ao registrar medições (RF09)" />
          <div className="space-y-3">
            <SelectField label="Temperatura" disabled={!p.canEdit} value={patient.units.temperature}
              options={[{ value: "CELSIUS", label: "°C" }, { value: "FAHRENHEIT", label: "°F" }]}
              onChange={(e) => void changeUnits.run(e.target.value as TemperatureUnit, patient.units.glucose, patient.units.weight)} />
            <SelectField label="Glicemia" disabled={!p.canEdit} value={patient.units.glucose}
              options={[{ value: "MG_PER_DL", label: "mg/dL" }, { value: "MMOL_PER_L", label: "mmol/L" }]}
              onChange={(e) => void changeUnits.run(patient.units.temperature, e.target.value as GlucoseUnit, patient.units.weight)} />
            <SelectField label="Peso" disabled={!p.canEdit} value={patient.units.weight}
              options={[{ value: "KILOGRAM", label: "kg" }, { value: "POUND", label: "lb" }]}
              onChange={(e) => void changeUnits.run(patient.units.temperature, patient.units.glucose, e.target.value as WeightUnit)} />
          </div>
          <ErrorMessage error={changeUnits.error} className="mt-3" />
        </Card>
        {p.canManageMembers && (
          <Card className="border-danger-200">
            <CardHeader title="Excluir dados do paciente" subtitle="Exclusão definitiva (LGPD) — não pode ser desfeita." />
            <Button variant="danger" size="sm" icon={<Trash2 className="h-3.5 w-3.5" />} onClick={() => setErasing(true)}>Excluir definitivamente</Button>
          </Card>
        )}
      </div>

      <Modal open={editing} title="Editar paciente" size="lg" onClose={() => setEditing(false)}>
        <PatientForm patient={patient} onCancel={() => setEditing(false)} onSubmit={async (data) => {
          await patientService.update(patient.id, data as UpdatePatientRequest);
          toast.success("Dados do paciente atualizados.");
          setEditing(false);
          reloadPatient();
        }} />
      </Modal>

      <EraseModal open={erasing} code={patient.publicCode} onClose={() => setErasing(false)} onConfirm={async (code, reason) => {
        await patientService.erase(patient.id, code, reason);
        toast.success("Os dados do paciente foram excluídos definitivamente.");
        navigate("/pacientes", { replace: true });
      }} />
    </div>
  );
}

function EraseModal({ open, code, onClose, onConfirm }: { open: boolean; code: string; onClose: () => void; onConfirm: (code: string, reason: string) => Promise<void> }) {
  const [typed, setTyped] = useState("");
  const [reason, setReason] = useState("");
  const action = useAction(onConfirm);
  return (
    <Modal open={open} title="Excluir definitivamente" onClose={onClose}>
      <form onSubmit={(e) => { e.preventDefault(); void action.run(typed, reason); }}>
        <p className="text-sm text-mist-500">
          Todos os registros clínicos, documentos e acessos deste paciente serão removidos. Para confirmar, digite o código <strong className="text-ink">{code}</strong>.
        </p>
        <div className="mt-4 space-y-4">
          <Input label="Código do paciente" required value={typed} onChange={(e) => setTyped(e.target.value)} />
          <Input label="Motivo" required value={reason} maxLength={500} onChange={(e) => setReason(e.target.value)} />
        </div>
        <ErrorMessage error={action.error} className="mt-4" />
        <FormActions onCancel={onClose} loading={action.pending} submitLabel="Excluir definitivamente" />
      </form>
    </Modal>
  );
}
