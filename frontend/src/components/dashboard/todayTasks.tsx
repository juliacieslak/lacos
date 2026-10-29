// agenda de hoje (WF01): cuidados programados para o dia, com marcação de concluídos.
import { CalendarCheck } from "lucide-react";
import { Link } from "react-router-dom";
import AgendaItemRow from "../agenda/agendaItemRow";
import { Card, CardHeader } from "../ui/card";
import EmptyState from "../ui/emptyState";
import { useAgendaActions } from "../../hooks/useAgendaActions";
import type { AgendaItem } from "../../types/appointment";
import type { PatientSummary } from "../../types/patient";

interface Props {
  items: AgendaItem[];
  pending: number;
  completed: number;
  patients: PatientSummary[];
  onChanged: () => void;
}

export default function TodayTasks({ items, pending, completed, patients, onChanged }: Props) {
  const { toggle, busyKey } = useAgendaActions(onChanged);
  const canAdd = (patientId: string) => patients.find((p) => p.id === patientId)?.myPermissions.canAdd ?? false;
  return (
    <Card>
      <CardHeader
        title="Agenda de hoje"
        subtitle={`${pending} pendente(s) · ${completed} concluído(s)`}
        icon={<CalendarCheck className="h-4 w-4" />}
        action={<Link to="/agenda" className="text-sm font-semibold text-lavender-600 hover:text-lavender-700">Ver agenda →</Link>}
      />
      {items.length === 0 ? (
        <EmptyState title="Nenhum cuidado programado para hoje" description="Cadastre medicamentos, rotinas ou compromissos para vê-los aqui." />
      ) : (
        <ul className="space-y-2">
          {items.map((item) => (
            <AgendaItemRow key={`${item.kind}:${item.sourceId}:${item.startsAt}`} item={item} canToggle={canAdd(item.patientId)}
              busy={busyKey === `${item.kind}:${item.sourceId}:${item.startsAt}`} onToggle={toggle} />
          ))}
        </ul>
      )}
    </Card>
  );
}
