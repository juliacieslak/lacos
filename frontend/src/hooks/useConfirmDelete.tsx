// padroniza a confirmação de exclusão (lógica, preserva histórico) em todas as abas.
import { useState } from "react";
import ConfirmDialog from "../components/ui/confirmDialog";
import { useToast } from "../contexts/toastContext";
import { useAction } from "./useAsync";

export function useConfirmDelete<T>(remove: (item: T) => Promise<unknown>, onDone: () => void, what = "este registro") {
  const [target, setTarget] = useState<T | null>(null);
  const toast = useToast();
  const action = useAction(async (item: T) => {
    await remove(item);
    toast.success("Registro excluído. O histórico de alterações foi preservado.");
    setTarget(null);
    onDone();
  });

  const element = (
    <ConfirmDialog open={target !== null} title="Confirmar exclusão" confirmLabel="Excluir"
      message={`Deseja excluir ${what}? A exclusão fica registrada no histórico do paciente.`}
      loading={action.pending} error={action.error}
      onClose={() => { setTarget(null); action.clearError(); }} onConfirm={() => target !== null && void action.run(target)} />
  );
  return { request: setTarget, element };
}
