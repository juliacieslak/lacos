// lista familiares, cuidadores e profissionais vinculados, com ações de gestão.
import { Pencil, UserMinus } from "lucide-react";
import PermissionBadge from "./permissionBadge";
import Badge from "../ui/badge";
import type { CareTeamMember } from "../../types/patientMember";
import { formatDate } from "../../utils/formatDate";
import { roleLabels } from "../../utils/labels";

interface Props {
  members: CareTeamMember[];
  currentUserId: string;
  canManage: boolean;
  onEdit: (m: CareTeamMember) => void;
  onRevoke: (m: CareTeamMember) => void;
}

export default function MemberList({ members, currentUserId, canManage, onEdit, onRevoke }: Props) {
  return (
    <ul className="divide-y divide-mist-100">
      {members.map((m) => {
        const isMe = m.userId === currentUserId;
        return (
          <li key={m.permissionId} className="flex flex-wrap items-center gap-3 py-3">
            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-sage-100 font-semibold text-sage-700">{m.name.charAt(0).toUpperCase()}</span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-ink">{m.name} {isMe && <span className="text-mist-400">(você)</span>}</p>
              <p className="truncate text-xs text-mist-500">{roleLabels[m.role]} · {m.relationshipType} · {m.email}</p>
              <p className="text-[11px] text-mist-400">Desde {formatDate(m.since)}</p>
            </div>
            {m.isActive ? <PermissionBadge permissions={m.permissions} /> : <Badge tone="danger">Acesso revogado</Badge>}
            {m.isActive && (canManage || isMe) && (
              <div className="flex gap-1">
                {canManage && <button type="button" onClick={() => onEdit(m)} aria-label={`Editar permissões de ${m.name}`} className="rounded-lg p-2 text-mist-400 hover:bg-lavender-50 hover:text-lavender-700"><Pencil className="h-4 w-4" /></button>}
                <button type="button" onClick={() => onRevoke(m)} aria-label={isMe ? "Sair da rede de cuidado" : `Revogar acesso de ${m.name}`} className="rounded-lg p-2 text-mist-400 hover:bg-danger-50 hover:text-danger-600"><UserMinus className="h-4 w-4" /></button>
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}
