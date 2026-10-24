// aba de medicamentos: cadastro, edição, horários, registro de doses e encerramento de uso (RF08 / US02).
import { Plus } from "lucide-react";
import { useState } from "react";
import MedicationForm from "../components/medications/medicationForm";
import MedicationList from "../components/medications/medicationList";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import FormActions from "../components/ui/formActions";
import { Textarea } from "../components/ui/input";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import Select from "../components/ui/select";
import { usePatientContext } from "../contexts/patientContext";
import { useToast } from "../contexts/toastContext";
import { useAction } from "../hooks/useAsync";
import { useConfirmDelete } from "../hooks/useConfirmDelete";
import { useMedications } from "../hooks/useMedications";
import { medicationService } from "../services/medicationService";
import type { Medication, MedicationRequest } from "../types/medication";

export default function MedicationsPage() {
  const { patient } = usePatientContext();
  const toast = useToast();
  const [activeOnly, setActiveOnly] = useState(true);
  const [editing, setEditing] = useState<Medication | "new" | null>(null);
  const [notGiven, setNotGiven] = useState<Medication | null>(null);
  const { data, loading, error, reload } = useMedications(patient.id, activeOnly);
  const del = useConfirmDelete((m: Medication) => medicationService.remove(patient.id, m.id), reload, "este medicamento");

  const save = async (form: MedicationRequest) => {
    if (editing === "new") await medicationService.create(patient.id, form);
    else if (editing) await medicationService.update(patient.id, editing.id, form);
    toast.success("Medicamento salvo.");
    setEditing(null);
    reload();
  };

  const registerDose = async (m: Medication, notes: string | null, administered: boolean) => {
    try {
      await medicationService.administrations.create(patient.id, {
        medicationId: m.id, scheduledFor: null, administeredAt: null, status: administered ? "ADMINISTERED" : "NOT_ADMINISTERED", notes,
      });
      toast.success(administered ? `Dose de ${m.name} registrada.` : `Registrado: ${m.name} não administrado.`);
      setNotGiven(null);
    } catch (e) {
      toast.error((e as Error).message);
    }
  };

  const discontinue = async (m: Medication) => {
    try {
      await medicationService.discontinue(patient.id, m.id);
      toast.success(`Uso de ${m.name} encerrado hoje.`);
      reload();
    } catch (e) {
      toast.error((e as Error).message);
    }
  };

  return (
    <Card>
      <CardHeader title="Medicamentos" subtitle="Doses programadas aparecem na agenda para marcação rápida."
        action={
          <div className="flex flex-wrap gap-2">
            <Select ariaLabel="Filtro de medicamentos" value={activeOnly ? "active" : "all"} onChange={(e) => setActiveOnly(e.target.value === "active")}
              options={[{ value: "active", label: "Em uso hoje" }, { value: "all", label: "Todos" }]} />
            {patient.myPermissions.canAdd && <Button icon={<Plus className="h-4 w-4" />} onClick={() => setEditing("new")}>Novo medicamento</Button>}
          </div>
        } />
      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : (
        <MedicationList medications={data ?? []} permissions={patient.myPermissions} onEdit={setEditing} onDelete={del.request}
          onDiscontinue={(m) => void discontinue(m)}
          onRegisterDose={(m, administered) => (administered ? void registerDose(m, null, true) : setNotGiven(m))} />
      )}

      <Modal open={editing !== null} title={editing === "new" ? "Novo medicamento" : "Editar medicamento"} size="lg" onClose={() => setEditing(null)}>
        {editing !== null && <MedicationForm medication={editing === "new" ? undefined : editing} onSubmit={save} onCancel={() => setEditing(null)} />}
      </Modal>
      <NotGivenModal medication={notGiven} onClose={() => setNotGiven(null)} onSubmit={(m, notes) => registerDose(m, notes, false)} />
      {del.element}
    </Card>
  );
}

// o backend exige o motivo quando a dose não foi administrada.
function NotGivenModal({ medication, onClose, onSubmit }: { medication: Medication | null; onClose: () => void; onSubmit: (m: Medication, notes: string) => Promise<void> }) {
  const [notes, setNotes] = useState("");
  const action = useAction(onSubmit);
  return (
    <Modal open={medication !== null} title={`Dose não administrada${medication ? `: ${medication.name}` : ""}`} onClose={onClose}>
      <form onSubmit={(e) => { e.preventDefault(); if (medication) void action.run(medication, notes); }}>
        <Textarea label="Motivo" required value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Ex.: paciente recusou, medicamento em falta" />
        <ErrorMessage error={action.error} className="mt-4" />
        <FormActions onCancel={onClose} loading={action.pending} submitLabel="Registrar" />
      </form>
    </Modal>
  );
}
