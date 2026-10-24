// lista os medicamentos do paciente.
import { Pill } from "lucide-react";
import MedicationCard from "./medicationCard";
import EmptyState from "../ui/emptyState";
import type { Permissions } from "../../types/common";
import type { Medication } from "../../types/medication";

interface Props {
  medications: Medication[];
  permissions: Permissions;
  onEdit: (m: Medication) => void;
  onDelete: (m: Medication) => void;
  onDiscontinue: (m: Medication) => void;
  onRegisterDose: (m: Medication, administered: boolean) => void;
}

export default function MedicationList({ medications, permissions, ...actions }: Props) {
  if (medications.length === 0) return <EmptyState icon={<Pill className="h-5 w-5" />} title="Nenhum medicamento cadastrado" />;
  return (
    <ul className="space-y-3">
      {medications.map((m) => (
        <MedicationCard key={m.id} medication={m} permissions={permissions} onEdit={() => actions.onEdit(m)} onDelete={() => actions.onDelete(m)}
          onDiscontinue={() => actions.onDiscontinue(m)} onRegisterDose={(a) => actions.onRegisterDose(m, a)} />
      ))}
    </ul>
  );
}
