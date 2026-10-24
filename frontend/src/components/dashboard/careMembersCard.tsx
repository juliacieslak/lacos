// "Quem está cuidando" (WF01): membros da rede de apoio do paciente.
import { HeartHandshake } from "lucide-react";
import { Link } from "react-router-dom";
import { Card, CardHeader } from "../ui/card";
import PermissionBadge from "../caremembers/permissionBadge";
import type { CareTeamMember } from "../../types/patientMember";
import { roleLabels } from "../../utils/labels";

export default function CareMembersCard({ members, patientId, canManage }: { members: CareTeamMember[]; patientId: string; canManage: boolean }) {
  return (
    <Card>
      <CardHeader title="Quem está cuidando" icon={<HeartHandshake className="h-4 w-4" />}
        action={canManage ? <Link to={`/pacientes/${patientId}/rede`} className="text-sm font-semibold text-lavender-600">Gerenciar permissões →</Link> : undefined} />
      <ul className="space-y-3">
        {members.map((m) => (
          <li key={m.permissionId} className="flex items-center gap-3">
            <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-sage-100 text-sm font-semibold text-sage-700">{m.name.charAt(0).toUpperCase()}</span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-ink">{m.name}</p>
              <p className="truncate text-xs text-mist-500">{roleLabels[m.role]} · {m.relationshipType}</p>
            </div>
            <PermissionBadge permissions={m.permissions} />
          </li>
        ))}
      </ul>
    </Card>
  );
}
