// aba "Rede de apoio": membros, permissões, convites e revogação de acesso (US01, UC04, UC05).
import { MailPlus, X } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import MemberForm from "../components/caremembers/memberForm";
import MemberList from "../components/caremembers/memberList";
import Badge from "../components/ui/badge";
import Button from "../components/ui/button";
import { Card, CardHeader } from "../components/ui/card";
import ConfirmDialog from "../components/ui/confirmDialog";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import { usePatientContext } from "../contexts/patientContext";
import { useSession } from "../contexts/sessionContext";
import { useToast } from "../contexts/toastContext";
import { useAction, useAsync } from "../hooks/useAsync";
import { careMemberService } from "../services/careMemberService";
import type { CareTeamMember, InviteMemberRequest, MemberRequest } from "../types/patientMember";
import { formatDate } from "../utils/formatDate";
import { invitationStatusLabels, roleLabels } from "../utils/labels";

export default function CareMembersPage() {
  const { patient } = usePatientContext();
  const { session } = useSession();
  const toast = useToast();
  const navigate = useNavigate();
  const canManage = patient.myPermissions.canManageMembers;
  const [showInactive, setShowInactive] = useState(false);
  const [editing, setEditing] = useState<CareTeamMember | "invite" | null>(null);
  const [revoking, setRevoking] = useState<CareTeamMember | null>(null);
  const members = useAsync((s) => careMemberService.members(patient.id, showInactive && canManage, s), [patient.id, showInactive]);
  const invitations = useAsync((s) => careMemberService.invitations(patient.id, s), [patient.id], canManage);
  const me = session!.user.id;

  const save = async (data: InviteMemberRequest | MemberRequest) => {
    if (editing === "invite") {
      await careMemberService.invite(patient.id, data as InviteMemberRequest);
      toast.success("Convite enviado.");
      invitations.reload();
    } else if (editing) {
      await careMemberService.updateMember(patient.id, editing.permissionId, data);
      toast.success("Permissões atualizadas. A mudança vale imediatamente.");
      members.reload();
    }
    setEditing(null);
  };

  const revoke = useAction(async (m: CareTeamMember) => {
    await careMemberService.revokeMember(patient.id, m.permissionId);
    setRevoking(null);
    if (m.userId === me) {
      toast.success("Você saiu da rede de cuidado.");
      navigate("/pacientes", { replace: true });
    } else {
      toast.success(`Acesso de ${m.name} revogado.`);
      members.reload();
    }
  });

  const cancelInvite = async (id: string) => {
    try {
      await careMemberService.revokeInvitation(patient.id, id);
      toast.success("Convite cancelado.");
      invitations.reload();
    } catch (e) {
      toast.error((e as Error).message);
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Quem está cuidando" subtitle="As permissões são aplicadas imediatamente."
          action={canManage && (
            <div className="flex flex-wrap items-center gap-2">
              <label className="flex items-center gap-2 text-xs text-mist-500">
                <input type="checkbox" className="accent-lavender-600" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} /> Mostrar revogados
              </label>
              <Button icon={<MailPlus className="h-4 w-4" />} onClick={() => setEditing("invite")}>Convidar</Button>
            </div>
          )} />
        {members.loading && !members.data ? <Loading /> : members.error ? <ErrorMessage error={members.error} onRetry={members.reload} /> : (
          <MemberList members={members.data ?? []} currentUserId={me} canManage={canManage} onEdit={setEditing} onRevoke={setRevoking} />
        )}
      </Card>

      {canManage && (
        <Card>
          <CardHeader title="Convites" />
          {invitations.loading && !invitations.data ? <Loading /> : invitations.error ? <ErrorMessage error={invitations.error} /> : (
            (invitations.data ?? []).length === 0 ? <p className="text-sm text-mist-500">Nenhum convite enviado.</p> : (
              <ul className="divide-y divide-mist-100">
                {invitations.data!.map((i) => (
                  <li key={i.id} className="flex flex-wrap items-center gap-3 py-3 text-sm">
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-semibold text-ink">{i.invitedEmail}</p>
                      <p className="text-xs text-mist-500">{roleLabels[i.role]} · {i.relationshipType} · expira em {formatDate(i.expiresAt)}</p>
                    </div>
                    <Badge tone={i.status === "PENDING" ? "warning" : i.status === "ACCEPTED" ? "sage" : "neutral"}>{invitationStatusLabels[i.status]}</Badge>
                    {i.status === "PENDING" && (
                      <button type="button" onClick={() => void cancelInvite(i.id)} className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-semibold text-danger-600 hover:bg-danger-50">
                        <X className="h-3.5 w-3.5" /> Cancelar
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )
          )}
        </Card>
      )}

      <Modal open={editing !== null} title={editing === "invite" ? "Convidar para a rede de cuidado" : "Editar permissões"} onClose={() => setEditing(null)}>
        {editing !== null && <MemberForm member={editing === "invite" ? undefined : editing} onSubmit={save} onCancel={() => setEditing(null)} />}
      </Modal>
      <ConfirmDialog open={revoking !== null} loading={revoke.pending} error={revoke.error}
        title={revoking?.userId === me ? "Sair da rede de cuidado" : "Revogar acesso"}
        message={revoking?.userId === me ? "Você deixará de acessar este paciente imediatamente." : `${revoking?.name} perderá o acesso a este paciente imediatamente.`}
        confirmLabel={revoking?.userId === me ? "Sair" : "Revogar"}
        onClose={() => { setRevoking(null); revoke.clearError(); }} onConfirm={() => revoking && void revoke.run(revoking)} />
    </div>
  );
}
