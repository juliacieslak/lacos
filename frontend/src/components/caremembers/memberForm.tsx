// formulário de convite / alteração de permissões de um membro (US01: e-mail do convidado e perfis
// "Apenas leitura" e "Edição"; RF07: permissões granulares de consultar, adicionar, alterar, excluir e gerenciar).
import { Mail } from "lucide-react";
import { useState, type FormEvent } from "react";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import Input, { SelectField } from "../ui/input";
import { useAction } from "../../hooks/useAsync";
import type { Permissions } from "../../types/common";
import type { AccessProfilePreset, CareRole, CareTeamMember, InviteMemberRequest, MemberRequest } from "../../types/patientMember";
import { presetLabels, roleLabels, toOptions } from "../../utils/labels";

const permissionLabels: [keyof Permissions, string][] = [
  ["canView", "Consultar informações"],
  ["canAdd", "Adicionar informações"],
  ["canEdit", "Alterar informações"],
  ["canDelete", "Excluir informações"],
  ["canManageMembers", "Gerenciar a rede de cuidado"],
];

interface Props {
  member?: CareTeamMember;
  onSubmit: (data: InviteMemberRequest | MemberRequest) => Promise<unknown>;
  onCancel: () => void;
}

export default function MemberForm({ member, onSubmit, onCancel }: Props) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<CareRole>(member?.role ?? "CAREGIVER");
  const [relationship, setRelationship] = useState(member?.relationshipType ?? "");
  const [preset, setPreset] = useState<AccessProfilePreset>(member ? "CUSTOM" : "READ_ONLY");
  const [permissions, setPermissions] = useState<Permissions>(member?.permissions ?? { canView: true, canAdd: false, canEdit: false, canDelete: false, canManageMembers: false });
  const save = useAction(onSubmit);
  const err = (k: string) => save.error?.fieldErrors[k]?.[0];

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    // o papel Administrador exige o perfil de administração.
    const finalPreset = role === "ADMINISTRATOR" && preset !== "CUSTOM" ? "MANAGER" : preset;
    const data: MemberRequest = { role, relationshipType: relationship, preset: finalPreset, permissions: finalPreset === "CUSTOM" ? permissions : null };
    void save.run(member ? data : { ...data, email });
  };

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        {!member && (
          <Input label="E-mail do convidado" type="email" required icon={<Mail className="h-4 w-4" />} value={email} onChange={(e) => setEmail(e.target.value)}
            error={err("email")} containerClassName="sm:col-span-2" hint="O convidado deve entrar no Laços com este e-mail (verificado) para aceitar." />
        )}
        <SelectField label="Papel no cuidado" options={toOptions(roleLabels)} value={role} onChange={(e) => setRole(e.target.value as CareRole)} />
        <Input label="Relação com o paciente" required value={relationship} maxLength={50} placeholder="Ex.: filho, cuidadora, médico" onChange={(e) => setRelationship(e.target.value)} error={err("relationshipType")} />
        <SelectField label="Perfil de acesso" options={toOptions(presetLabels)} value={preset} onChange={(e) => setPreset(e.target.value as AccessProfilePreset)} containerClassName="sm:col-span-2" />
      </div>
      {preset === "CUSTOM" && (
        <fieldset className="mt-4 space-y-2 rounded-xl border border-mist-200 p-4">
          <legend className="px-1 text-xs font-semibold uppercase tracking-[0.06em] text-mist-500">Permissões</legend>
          {permissionLabels.map(([key, label]) => (
            <label key={key} className="flex items-center gap-3 text-sm text-mist-700">
              <input type="checkbox" className="h-4 w-4 accent-lavender-600" checked={permissions[key]} onChange={(e) => setPermissions({ ...permissions, [key]: e.target.checked })} />
              {label}
            </label>
          ))}
          <p className="text-xs text-mist-500">Quem pode adicionar, alterar ou excluir também passa a poder consultar.</p>
        </fieldset>
      )}
      <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} submitLabel={member ? "Salvar permissões" : "Enviar convite"} />
    </form>
  );
}
