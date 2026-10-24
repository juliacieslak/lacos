// aba "Documentos": upload de exames, guias e receitas (RF19/US03) — PDF, JPG ou PNG até 10 MB —
// com pré-visualização segura (o arquivo é baixado com o token e exibido por URL temporária).
import { Download, Eye, FileText, Trash2, Upload } from "lucide-react";
import { useEffect, useRef, useState, type FormEvent } from "react";
import Badge from "../components/ui/badge";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import FormActions from "../components/ui/formActions";
import Input, { SelectField } from "../components/ui/input";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import Pagination from "../components/ui/pagination";
import RecordMeta from "../components/ui/recordMeta";
import { usePatientContext } from "../contexts/patientContext";
import { useToast } from "../contexts/toastContext";
import { toApiError, useAction, useAsync } from "../hooks/useAsync";
import { useConfirmDelete } from "../hooks/useConfirmDelete";
import { ACCEPTED_DOCUMENT_TYPES, MAX_DOCUMENT_BYTES, documentService } from "../services/documentService";
import type { DocumentCategory, PatientDocument } from "../types/document";
import { formatBytes } from "../utils/formatMeasurement";
import { documentCategoryLabels, toOptions } from "../utils/labels";

export default function DocumentsPage() {
  const { patient } = usePatientContext();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [category, setCategory] = useState<DocumentCategory | "">("");
  const [uploading, setUploading] = useState(false);
  const [preview, setPreview] = useState<{ doc: PatientDocument; url: string } | null>(null);
  const { data, loading, error, reload } = useAsync((s) => documentService.list(patient.id, category || undefined, page, s), [patient.id, category, page]);
  const del = useConfirmDelete((d: PatientDocument) => documentService.remove(patient.id, d.id), reload, "este documento");

  // libera a URL temporária ao fechar a pré-visualização.
  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview.url); }, [preview]);

  const open = async (doc: PatientDocument, download: boolean) => {
    try {
      const blob = await documentService.content(patient.id, doc.id);
      const url = URL.createObjectURL(blob);
      if (download) {
        const a = document.createElement("a");
        a.href = url;
        a.download = doc.fileName;
        a.click();
        window.setTimeout(() => URL.revokeObjectURL(url), 1000);
      } else setPreview({ doc, url });
    } catch (e) {
      toast.error(toApiError(e).message);
    }
  };

  return (
    <Card>
      <CardHeader title="Documentos" subtitle="Exames, guias médicas e receitas" icon={<FileText className="h-4 w-4" />}
        action={
          <div className="flex flex-wrap gap-2">
            <select aria-label="Filtrar por categoria" value={category} onChange={(e) => { setCategory(e.target.value as DocumentCategory | ""); setPage(1); }}
              className="rounded-xl border border-mist-200 bg-white px-3 py-2 text-sm">
              <option value="">Todas as categorias</option>
              {toOptions(documentCategoryLabels).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
            {patient.myPermissions.canAdd && <Button icon={<Upload className="h-4 w-4" />} onClick={() => setUploading(true)}>Enviar documento</Button>}
          </div>
        } />
      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : !data || data.items.length === 0 ? (
        <EmptyState icon={<FileText className="h-5 w-5" />} title="Nenhum documento anexado" />
      ) : (
        <>
          <ul className="space-y-3">
            {data.items.map((d) => (
              <li key={d.id} className="flex flex-wrap items-center gap-3 rounded-2xl border border-mist-200 bg-white p-4">
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-serene-100 text-serene-700"><FileText className="h-5 w-5" /></span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-ink">{d.fileName}</p>
                  <div className="mt-1 flex flex-wrap gap-1.5"><Badge tone="serene">{documentCategoryLabels[d.category]}</Badge><Badge tone="neutral">{formatBytes(d.sizeBytes)}</Badge></div>
                  {d.description && <p className="mt-1 text-sm text-mist-700">{d.description}</p>}
                  <div className="mt-1"><RecordMeta record={d.record} /></div>
                </div>
                <div className="flex gap-1">
                  <button type="button" onClick={() => void open(d, false)} aria-label={`Visualizar ${d.fileName}`} className="rounded-lg p-2 text-mist-400 hover:bg-lavender-50 hover:text-lavender-700"><Eye className="h-4 w-4" /></button>
                  <button type="button" onClick={() => void open(d, true)} aria-label={`Baixar ${d.fileName}`} className="rounded-lg p-2 text-mist-400 hover:bg-lavender-50 hover:text-lavender-700"><Download className="h-4 w-4" /></button>
                  {patient.myPermissions.canDelete && <button type="button" onClick={() => del.request(d)} aria-label={`Excluir ${d.fileName}`} className="rounded-lg p-2 text-mist-400 hover:bg-danger-50 hover:text-danger-600"><Trash2 className="h-4 w-4" /></button>}
                </div>
              </li>
            ))}
          </ul>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      )}

      <Modal open={uploading} title="Enviar documento" onClose={() => setUploading(false)}>
        <UploadForm onCancel={() => setUploading(false)} onSubmit={async (file, cat, description) => {
          await documentService.upload(patient.id, file, cat, description);
          toast.success("Documento enviado.");
          setUploading(false);
          setPage(1);
          reload();
        }} />
      </Modal>
      <Modal open={preview !== null} title={preview?.doc.fileName ?? ""} size="lg" onClose={() => setPreview(null)}>
        {preview && (preview.doc.contentType === "application/pdf"
          ? <iframe title={preview.doc.fileName} src={preview.url} className="h-[70vh] w-full rounded-xl border border-mist-200" />
          : <img src={preview.url} alt={preview.doc.description ?? preview.doc.fileName} className="mx-auto max-h-[70vh] rounded-xl" />)}
      </Modal>
      {del.element}
    </Card>
  );
}

function UploadForm({ onSubmit, onCancel }: { onSubmit: (file: File, category: DocumentCategory, description?: string) => Promise<void>; onCancel: () => void }) {
  const [file, setFile] = useState<File | null>(null);
  const [category, setCategory] = useState<DocumentCategory>("EXAM");
  const [description, setDescription] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const action = useAction(onSubmit);

  // validação antecipada (o backend valida novamente, inclusive a assinatura binária do arquivo).
  const pick = (f: File | null) => {
    setLocalError(null);
    if (f && !ACCEPTED_DOCUMENT_TYPES.includes(f.type)) return setLocalError("Formato não permitido. Envie PDF, JPG ou PNG.");
    if (f && f.size > MAX_DOCUMENT_BYTES) return setLocalError("O arquivo excede o limite de 10 MB.");
    setFile(f);
  };

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (!file) return setLocalError("Selecione um arquivo.");
    void action.run(file, category, description || undefined);
  };

  return (
    <form onSubmit={submit}>
      <button type="button" onClick={() => inputRef.current?.click()} className="flex w-full flex-col items-center gap-2 rounded-2xl border-2 border-dashed border-lavender-300 bg-lavender-50 px-4 py-8 text-sm text-lavender-700 hover:bg-lavender-100">
        <Upload className="h-6 w-6" />
        {file ? <span className="font-semibold">{file.name} ({formatBytes(file.size)})</span> : <span>Clique para escolher um arquivo (PDF, JPG ou PNG até 10 MB)</span>}
      </button>
      <input ref={inputRef} type="file" accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png" className="hidden" onChange={(e) => pick(e.target.files?.[0] ?? null)} />
      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <SelectField label="Categoria" options={toOptions(documentCategoryLabels)} value={category} onChange={(e) => setCategory(e.target.value as DocumentCategory)} />
        <Input label="Descrição" value={description} maxLength={1000} onChange={(e) => setDescription(e.target.value)} />
      </div>
      <ErrorMessage error={localError ?? action.error} className="mt-4" />
      <FormActions onCancel={onCancel} loading={action.pending} submitLabel="Enviar" />
    </form>
  );
}
