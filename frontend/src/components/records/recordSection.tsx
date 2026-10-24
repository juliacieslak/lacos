// seção genérica (lista paginada + criar/editar/excluir) para registros clínicos que seguem o mesmo contrato da API
// (sintomas, diário, condições, cirurgias, alergias). Evita duplicar a mesma lógica em cada aba.
import { Pencil, Plus, Trash2 } from "lucide-react";
import { useState, type ReactNode } from "react";
import Button from "../ui/button";
import { Card, CardHeader } from "../ui/card";
import EmptyState from "../ui/emptyState";
import ErrorMessage from "../ui/errorMessage";
import Loading from "../ui/loading";
import Modal from "../ui/modal";
import Pagination from "../ui/pagination";
import RecordMeta from "../ui/recordMeta";
import { useToast } from "../../contexts/toastContext";
import { useAsync } from "../../hooks/useAsync";
import { useConfirmDelete } from "../../hooks/useConfirmDelete";
import type { createRecordService } from "../../services/recordService";
import type { Permissions, RecordInfo } from "../../types/common";

interface RecordSectionProps<T extends { id: string; record: RecordInfo }, R> {
  title: string;
  subtitle?: string;
  icon?: ReactNode;
  emptyText: string;
  patientId: string;
  permissions: Permissions;
  service: ReturnType<typeof createRecordService<T, R>>;
  renderItem: (item: T) => ReactNode;
  renderForm: (props: { item?: T; onSubmit: (data: R) => Promise<void>; onCancel: () => void }) => ReactNode;
}

export default function RecordSection<T extends { id: string; record: RecordInfo }, R>(props: RecordSectionProps<T, R>) {
  const { title, subtitle, icon, emptyText, patientId, permissions, service, renderItem, renderForm } = props;
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<T | "new" | null>(null);
  const { data, loading, error, reload } = useAsync((s) => service.list(patientId, { page, pageSize: 10 }, s), [patientId, page]);
  const del = useConfirmDelete((item: T) => service.remove(patientId, item.id), reload);

  const submit = async (form: R) => {
    if (editing === "new") await service.create(patientId, form);
    else if (editing) await service.update(patientId, editing.id, form);
    toast.success(`${title}: registro salvo.`);
    setEditing(null);
    reload();
  };

  return (
    <Card>
      <CardHeader title={title} subtitle={subtitle} icon={icon}
        action={permissions.canAdd && <Button size="sm" icon={<Plus className="h-3.5 w-3.5" />} onClick={() => setEditing("new")}>Adicionar</Button>} />
      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : !data || data.items.length === 0 ? (
        <EmptyState title={emptyText} />
      ) : (
        <>
          <ul className="space-y-3">
            {data.items.map((item) => (
              <li key={item.id} className="flex items-start gap-3 rounded-2xl border border-mist-200 bg-white p-4">
                <div className="min-w-0 flex-1">
                  {renderItem(item)}
                  <div className="mt-2"><RecordMeta record={item.record} /></div>
                </div>
                <div className="flex gap-1">
                  {permissions.canEdit && <button type="button" onClick={() => setEditing(item)} aria-label="Editar" className="rounded-lg p-2 text-mist-400 hover:bg-lavender-50 hover:text-lavender-700"><Pencil className="h-4 w-4" /></button>}
                  {permissions.canDelete && <button type="button" onClick={() => del.request(item)} aria-label="Excluir" className="rounded-lg p-2 text-mist-400 hover:bg-danger-50 hover:text-danger-600"><Trash2 className="h-4 w-4" /></button>}
                </div>
              </li>
            ))}
          </ul>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      )}
      <Modal open={editing !== null} title={editing === "new" ? `${title}: novo registro` : `${title}: editar`} onClose={() => setEditing(null)}>
        {editing !== null && renderForm({ item: editing === "new" ? undefined : editing, onSubmit: submit, onCancel: () => setEditing(null) })}
      </Modal>
      {del.element}
    </Card>
  );
}
