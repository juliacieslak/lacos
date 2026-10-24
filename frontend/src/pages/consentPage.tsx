// novo aceite LGPD quando a versão dos termos muda ou o consentimento foi revogado (RNF01).
import { ShieldCheck } from "lucide-react";
import { useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import Button from "../components/ui/button";
import ErrorMessage from "../components/ui/errorMessage";
import AuthCard from "../layouts/authLayout";
import { useSession } from "../contexts/sessionContext";
import { useAction } from "../hooks/useAsync";
import { useAuth } from "../hooks/useAuth";
import { sessionService } from "../services/sessionService";

export default function ConsentPage() {
  const { session, refresh } = useSession();
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [terms, setTerms] = useState(false);
  const [sensitive, setSensitive] = useState(false);
  const accept = useAction(sessionService.acceptConsents);

  if (session?.consentsUpToDate) return <Navigate to="/dashboard" replace />;

  const submit = async () => {
    if (await accept.run(terms, sensitive)) {
      await refresh();
      navigate("/dashboard", { replace: true });
    }
  };

  return (
    <AuthCard>
      <ShieldCheck className="h-8 w-8 text-sage-700" />
      <h1 className="mt-4 font-display text-2xl font-semibold text-ink">Atualização de privacidade</h1>
      <p className="mt-2 text-sm text-mist-500">Para continuar acessando dados de saúde, confirme os termos vigentes.</p>
      <div className="mt-6 space-y-3">
        <label className="flex items-start gap-3 text-sm text-mist-700">
          <input type="checkbox" checked={terms} onChange={(e) => setTerms(e.target.checked)} className="mt-1 h-4 w-4 accent-lavender-600" />
          Li e aceito os termos de uso.
        </label>
        <label className="flex items-start gap-3 text-sm text-mist-700">
          <input type="checkbox" checked={sensitive} onChange={(e) => setSensitive(e.target.checked)} className="mt-1 h-4 w-4 accent-lavender-600" />
          Autorizo o tratamento de dados sensíveis de saúde.
        </label>
      </div>
      <ErrorMessage error={accept.error} className="mt-4" />
      <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
        <Button variant="ghost" onClick={() => void logout()}>Sair</Button>
        <Button disabled={!terms || !sensitive} loading={accept.pending} onClick={() => void submit()}>Confirmar</Button>
      </div>
    </AuthCard>
  );
}
