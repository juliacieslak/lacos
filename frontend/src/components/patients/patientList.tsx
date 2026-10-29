// lista os pacientes vinculados ao usuário, com estado vazio.
import { Users } from "lucide-react";
import PatientCard from "./patientCard";
import EmptyState from "../ui/emptyState";
import type { PatientSummary } from "../../types/patient";

export default function PatientList({ patients, filtered }: { patients: PatientSummary[]; filtered: boolean }) {
  if (patients.length === 0)
    return (
      <EmptyState icon={<Users className="h-5 w-5" />}
        title={filtered ? "Nenhum paciente encontrado" : "Você ainda não acompanha nenhum paciente"}
        description={filtered ? "Ajuste a busca ou o filtro de status." : "Cadastre um paciente ou aceite um convite de uma rede de cuidado."} />
    );
  return <div className="space-y-3">{patients.map((p) => <PatientCard key={p.id} patient={p} />)}</div>;
}
