// cabeçalho da página de detalhes do paciente: nome, idade, código, status e papel do usuário.
import Badge from "../ui/badge";
import PermissionBadge from "../caremembers/permissionBadge";
import type { Patient } from "../../types/patient";
import { careStatusLabels, roleLabels } from "../../utils/labels";

export default function PatientHeader({ patient }: { patient: Patient }) {
  return (
    <div className="flex flex-wrap items-center gap-4">
      <span className="grid h-14 w-14 shrink-0 place-items-center rounded-full bg-lavender-200 font-display text-xl font-semibold text-lavender-700">
        {patient.fullName.charAt(0).toUpperCase()}
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-xs font-medium uppercase tracking-[0.08em] text-lavender-600">Paciente · {patient.publicCode}</p>
        <h1 className="font-display text-2xl font-semibold text-ink">{patient.fullName}</h1>
        <div className="mt-1.5 flex flex-wrap gap-1.5">
          <Badge tone="neutral">{patient.age} anos</Badge>
          <Badge tone={patient.careStatus === "IN_FOLLOW_UP" ? "sage" : "neutral"}>{careStatusLabels[patient.careStatus]}</Badge>
          <Badge tone="lavender">Você: {roleLabels[patient.myRole]}</Badge>
          <PermissionBadge permissions={patient.myPermissions} />
        </div>
      </div>
    </div>
  );
}
