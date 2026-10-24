// convites recebidos ("Tela de acesso pendente" do fluxo de navegação): aceitar ou recusar.
import { Check, MailOpen, X } from "lucide-react";
import { useNavigate } from "react-router-dom";
import PermissionBadge from "../components/caremembers/permissionBadge";
import Button from "../components/ui/button";
import { Card, PageHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import { useSession } from "../contexts/sessionContext";
import { useToast } from "../contexts/toastContext";
import { useAction, useAsync } from "../hooks/useAsync";
import { careMemberService } from "../services/careMemberService";
import { formatDate } from "../utils/formatDate";
import { roleLabels } from "../utils/labels";

export default function InvitationsPage() {
  const { refresh } = useSession();
  const toast = useToast();
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync((s) => careMemberService.myInvitations(s), []);

  const accept = useAction(async (id: string, patientId: string) => {
    await careMemberService.accept(id);
    await refresh();
    toast.success("Convite aceito. Bem-vindo(a) à rede de cuidado!");
    navigate(`/pacientes/${patientId}`);
  });
  const decline = useAction(async (id: string) => {
    await careMemberService.decline(id);
    await refresh();
    toast.success("Convite recusado.");
    reload();
  });

  return (
    <>
      <PageHeader eyebrow="Convites" title="Convites recebidos" description="Aceite para acessar o paciente com as permissões definidas pelo administrador." />
      <ErrorMessage error={accept.error ?? decline.error} className="mb-4" />
      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : !data?.length ? (
        <EmptyState icon={<MailOpen className="h-5 w-5" />} title="Nenhum convite pendente"
          description="Peça ao administrador do paciente para convidar o e-mail da sua conta. Convites só aparecem para e-mails verificados." />
      ) : (
        <div className="space-y-3">
          {data.map((i) => (
            <Card key={i.id}>
              <div className="flex flex-wrap items-center gap-4">
                <div className="min-w-0 flex-1">
                  <p className="font-display font-semibold text-ink">{i.patientName}</p>
                  <p className="text-sm text-mist-500">Convite de {i.invitedByName ?? "—"} · {roleLabels[i.role]} ({i.relationshipType}) · válido até {formatDate(i.expiresAt)}</p>
                  <div className="mt-2"><PermissionBadge permissions={i.permissions} /></div>
                </div>
                <div className="flex gap-2">
                  <Button variant="secondary" icon={<X className="h-4 w-4" />} loading={decline.pending} onClick={() => void decline.run(i.id)}>Recusar</Button>
                  <Button icon={<Check className="h-4 w-4" />} loading={accept.pending} onClick={() => void accept.run(i.id, i.patientId)}>Aceitar</Button>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </>
  );
}
