// resumo do paciente selecionado: nome, idade, status, condições e cuidados pendentes.
import { CalendarClock, ChevronRight } from "lucide-react";
import { Link } from "react-router-dom";
import Badge from "../ui/badge";
import type { PatientSummary as Summary } from "../../types/patient";
import { formatDateTime } from "../../utils/formatDate";
import { careStatusLabels } from "../../utils/labels";

export default function PatientSummary({ patient }: { patient: Summary }) {
  return (
    <Link to={`/pacientes/${patient.id}`} className="group flex items-center gap-4 rounded-[var(--radius-card)] border border-mist-200 bg-white p-4 shadow-[var(--shadow-card)] transition hover:border-lavender-300 sm:p-5">
      <span className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-lavender-200 font-display text-lg font-semibold text-lavender-700">
        {patient.fullName.charAt(0).toUpperCase()}
      </span>
      <div className="min-w-0 flex-1">
        <p className="font-display font-semibold text-ink">{patient.fullName}</p>
        <p className="truncate text-xs text-mist-500">
          {patient.age} anos{patient.activeConditions.length > 0 && ` · ${patient.activeConditions.join(", ")}`}
        </p>
        <div className="mt-2 flex flex-wrap gap-1.5">
          <Badge tone={patient.careStatus === "IN_FOLLOW_UP" ? "sage" : "neutral"}>{careStatusLabels[patient.careStatus]}</Badge>
          <Badge tone={patient.todayPendingCareItems > 0 ? "warning" : "lavender"}>Cuidados de hoje ({patient.todayPendingCareItems}/{patient.todayCareItems})</Badge>
          {patient.nextAppointmentAt && (
            <Badge tone="serene"><CalendarClock className="h-3 w-3" /> Próxima consulta: {formatDateTime(patient.nextAppointmentAt)}</Badge>
          )}
        </div>
      </div>
      <ChevronRight className="h-5 w-5 shrink-0 text-mist-400 transition group-hover:translate-x-0.5 group-hover:text-lavender-600" />
    </Link>
  );
}
