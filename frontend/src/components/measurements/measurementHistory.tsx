// lista paginada do histórico de medições do paciente.
import { Activity } from "lucide-react";
import MeasurementCard from "./measurementCard";
import EmptyState from "../ui/emptyState";
import Pagination from "../ui/pagination";
import type { PagedResult } from "../../types/common";
import type { HealthMeasurement } from "../../types/healthMeasurement";

interface Props {
  data: PagedResult<HealthMeasurement>;
  onPageChange: (page: number) => void;
  onDelete?: (m: HealthMeasurement) => void;
}

export default function MeasurementHistory({ data, onPageChange, onDelete }: Props) {
  if (data.items.length === 0) return <EmptyState icon={<Activity className="h-5 w-5" />} title="Nenhuma medição registrada" description="Registre pressão, glicemia e outros sinais vitais." />;
  return (
    <>
      <ul className="space-y-3">{data.items.map((m) => <MeasurementCard key={m.id} measurement={m} onDelete={onDelete && (() => onDelete(m))} />)}</ul>
      <Pagination page={data.page} totalPages={data.totalPages} onChange={onPageChange} />
    </>
  );
}
