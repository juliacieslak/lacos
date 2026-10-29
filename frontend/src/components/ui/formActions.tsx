// rodapé padrão dos formulários em modal.
import Button from "./button";

export default function FormActions({ onCancel, loading, submitLabel = "Salvar" }: { onCancel: () => void; loading?: boolean; submitLabel?: string }) {
  return (
    <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
      <Button variant="secondary" onClick={onCancel}>Cancelar</Button>
      <Button type="submit" loading={loading}>{submitLabel}</Button>
    </div>
  );
}
