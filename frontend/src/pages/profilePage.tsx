// perfil do usuário: dados cadastrais, consentimentos LGPD e exclusão definitiva da conta (RNF01).
import { LogOut, ShieldCheck, Trash2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import ProfileFieldsForm from "../components/profile/profileFields";
import Badge from "../components/ui/badge";
import Button from "../components/ui/button";
import { Card, CardHeader, PageHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import FormActions from "../components/ui/formActions";
import Input from "../components/ui/input";
import Modal from "../components/ui/modal";
import { useSession } from "../contexts/sessionContext";
import { useToast } from "../contexts/toastContext";
import { useAction, useAsync } from "../hooks/useAsync";
import { useAuth } from "../hooks/useAuth";
import { sessionService } from "../services/sessionService";
import type { ProfileFields } from "../types/user";
import { formatCep } from "../utils/formatCep";
import { formatDateTime } from "../utils/formatDate";
import { formatPhone } from "../utils/validation";

export default function ProfilePage() {
  const { session, updateUser } = useSession();
  const { logout } = useAuth();
  const toast = useToast();
  const user = session!.user;
  const [form, setForm] = useState<ProfileFields>({
    profileType: user.profileType ?? "FAMILY", name: user.name, birthDate: user.birthDate ?? "", phone: formatPhone(user.phone ?? ""),
    profession: user.profession ?? "", cep: formatCep(user.cep ?? ""), ethnicity: user.ethnicity ?? "NAO_DECLARADA", gender: user.gender ?? "PREFER_NOT_TO_SAY",
  });
  const [deleting, setDeleting] = useState(false);
  const consents = useAsync(() => sessionService.consents(), []);
  const save = useAction(async () => {
    updateUser(await sessionService.updateProfile(form));
    toast.success("Perfil atualizado.");
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    void save.run();
  };

  return (
    <>
      <PageHeader eyebrow="Conta" title="Meu perfil" description={`${user.email} · código ${user.publicCode}`}
        action={<Button variant="secondary" icon={<LogOut className="h-4 w-4" />} onClick={() => void logout()}>Sair</Button>} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
        <Card>
          <CardHeader title="Dados cadastrais" subtitle={`CPF ${user.cpfMasked ?? "—"} · e-mail gerenciado pelo Auth0`} />
          <form onSubmit={submit} noValidate>
            <ProfileFieldsForm value={form} onChange={setForm} errors={save.error?.fieldErrors ?? {}} />
            <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} className="mt-4" />
            <div className="mt-6 flex justify-end"><Button type="submit" loading={save.pending}>Salvar alterações</Button></div>
          </form>
        </Card>
        <div className="space-y-6">
          <Card>
            <CardHeader title="Privacidade (LGPD)" icon={<ShieldCheck className="h-4 w-4" />} />
            {consents.data?.consents.filter((c) => c.isCurrent).map((c) => (
              <div key={c.consentType} className="mb-2 flex items-center justify-between gap-2 text-sm">
                <span>{c.consentType === "TERMS_OF_USE" ? "Termos de uso" : "Dados sensíveis de saúde"} (v{c.version})</span>
                <Badge tone="sage">Aceito em {formatDateTime(c.acceptedAt)}</Badge>
              </div>
            ))}
            <ErrorMessage error={consents.error} />
          </Card>
          <Card className="border-danger-200">
            <CardHeader title="Excluir minha conta" subtitle="Seus dados pessoais serão anonimizados e a conta removida." />
            <Button variant="danger" size="sm" icon={<Trash2 className="h-3.5 w-3.5" />} onClick={() => setDeleting(true)}>Excluir conta</Button>
          </Card>
        </div>
      </div>
      <DeleteAccountModal open={deleting} onClose={() => setDeleting(false)} onDeleted={() => void logout()} />
    </>
  );
}

function DeleteAccountModal({ open, onClose, onDeleted }: { open: boolean; onClose: () => void; onDeleted: () => void }) {
  const [confirmation, setConfirmation] = useState("");
  const [reason, setReason] = useState("");
  const action = useAction(async () => {
    await sessionService.deleteAccount(confirmation, reason || null);
    onDeleted();
  });
  return (
    <Modal open={open} title="Excluir conta definitivamente" onClose={onClose}>
      <form onSubmit={(e) => { e.preventDefault(); void action.run(); }}>
        <p className="text-sm text-mist-500">Se você for o único administrador de algum paciente, transfira a administração ou exclua o paciente antes. Digite <strong className="text-ink">EXCLUIR</strong> para confirmar.</p>
        <div className="mt-4 space-y-4">
          <Input label="Confirmação" required value={confirmation} onChange={(e) => setConfirmation(e.target.value)} />
          <Input label="Motivo (opcional)" value={reason} maxLength={500} onChange={(e) => setReason(e.target.value)} />
        </div>
        <ErrorMessage error={action.error} className="mt-4" />
        <FormActions onCancel={onClose} loading={action.pending} submitLabel="Excluir conta" />
      </form>
    </Modal>
  );
}
