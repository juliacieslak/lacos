// confirmação para ações destrutivas (excluir registro, revogar acesso).
import Button from "./button";
import ErrorMessage from "./errorMessage";
import Modal from "./modal";
import type { ApiError } from "../../services/api";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  loading?: boolean;
  error?: ApiError | null;
  onConfirm: () => void;
  onClose: () => void;
}

export default function ConfirmDialog({ open, title, message, confirmLabel = "Confirmar", loading, error, onConfirm, onClose }: ConfirmDialogProps) {
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <p className="text-sm text-mist-500">{message}</p>
      <ErrorMessage error={error} className="mt-4" />
      <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button variant="secondary" onClick={onClose}>Cancelar</Button>
        <Button variant="danger" loading={loading} onClick={onConfirm}>{confirmLabel}</Button>
      </div>
    </Modal>
  );
}
