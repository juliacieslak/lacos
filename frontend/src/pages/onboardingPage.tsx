// primeiro acesso: conclusão do cadastro (RF01) com aceite dos termos e do tratamento de dados sensíveis (RNF01).
import { IdCard, ShieldCheck } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import ProfileFieldsForm from "../components/profile/profileFields";
import Button from "../components/ui/button";
import ErrorMessage from "../components/ui/errorMessage";
import Input from "../components/ui/input";
import Logo from "../components/ui/logo";
import AuthCard from "../layouts/authLayout";
import { useSession } from "../contexts/sessionContext";
import { useAction } from "../hooks/useAsync";
import { useAuth } from "../hooks/useAuth";
import { sessionService } from "../services/sessionService";
import type { ProfileFields } from "../types/user";
import { isValidCep } from "../utils/formatCep";
import { formatCpf, isValidCpf, isValidPhone } from "../utils/validation";

export default function OnboardingPage() {
  const { session, refresh } = useSession();
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<ProfileFields>({
    profileType: "FAMILY", name: session?.user.name ?? "", birthDate: "", phone: "", profession: "", cep: "",
    ethnicity: "NAO_DECLARADA", gender: "PREFER_NOT_TO_SAY",
  });
  const [cpf, setCpf] = useState("");
  const [terms, setTerms] = useState(false);
  const [sensitive, setSensitive] = useState(false);
  const [localErrors, setLocalErrors] = useState<Record<string, string[]>>({});
  const save = useAction(sessionService.completeOnboarding);

  if (session?.user.profileCompleted) return <Navigate to="/dashboard" replace />;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    // validação no cliente para resposta imediata; o backend valida novamente.
    const errors: Record<string, string[]> = {};
    if (!isValidCpf(cpf)) errors.cpf = ["CPF inválido."];
    if (!isValidCep(profile.cep)) errors.cep = ["CEP inválido."];
    if (!isValidPhone(profile.phone)) errors.phone = ["Telefone inválido. Informe DDD + número."];
    if (!terms || !sensitive) errors.consent = ["É necessário aceitar os termos e o tratamento de dados de saúde."];
    setLocalErrors(errors);
    if (Object.keys(errors).length) return;

    const user = await save.run({ ...profile, cpf, acceptTermsOfUse: terms, acceptSensitiveDataProcessing: sensitive });
    if (user) {
      await refresh();
      navigate("/dashboard", { replace: true });
    }
  };

  const errors = { ...(save.error?.fieldErrors ?? {}), ...localErrors };

  return (
    <AuthCard wide>
      <div className="flex items-center gap-4">
        <Logo width={64} />
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink">Complete seu cadastro</h1>
          <p className="text-sm text-mist-500">Precisamos de alguns dados para organizar o cuidado com segurança.</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="mt-8 space-y-6" noValidate>
        <ProfileFieldsForm value={profile} onChange={setProfile} errors={errors} />
        <Input label="CPF" required icon={<IdCard className="h-4 w-4" />} value={cpf} inputMode="numeric" placeholder="000.000.000-00"
          onChange={(e) => setCpf(formatCpf(e.target.value))} error={errors.cpf?.[0]} hint="Armazenado com criptografia e exibido sempre mascarado." />

        <fieldset className="space-y-3 rounded-2xl border border-mist-200 bg-mist-50 p-4">
          <legend className="flex items-center gap-2 px-1 text-sm font-semibold text-ink"><ShieldCheck className="h-4 w-4 text-sage-700" /> Privacidade (LGPD)</legend>
          <label className="flex items-start gap-3 text-sm text-mist-700">
            <input type="checkbox" checked={terms} onChange={(e) => setTerms(e.target.checked)} className="mt-1 h-4 w-4 accent-lavender-600" />
            Li e aceito os termos de uso do Laços.
          </label>
          <label className="flex items-start gap-3 text-sm text-mist-700">
            <input type="checkbox" checked={sensitive} onChange={(e) => setSensitive(e.target.checked)} className="mt-1 h-4 w-4 accent-lavender-600" />
            Autorizo o tratamento de dados sensíveis de saúde para a organização dos cuidados.
          </label>
          {errors.consent && <p className="text-xs text-danger-600">{errors.consent[0]}</p>}
        </fieldset>

        <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} />

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
          <Button variant="ghost" onClick={() => void logout()}>Sair</Button>
          <Button type="submit" size="lg" loading={save.pending}>Concluir cadastro</Button>
        </div>
      </form>
    </AuthCard>
  );
}
