// "registrado por ... em ..." — atende US04 (exibir quem realizou cada registro).
import type { RecordInfo } from "../../types/common";
import { formatDateTime } from "../../utils/formatDate";

export default function RecordMeta({ record }: { record: RecordInfo }) {
  return (
    <p className="text-[11px] text-mist-400">
      Registrado por {record.createdByName ?? "—"} em {formatDateTime(record.createdAt)}
      {record.updatedByName && ` · editado por ${record.updatedByName}`}
    </p>
  );
}
