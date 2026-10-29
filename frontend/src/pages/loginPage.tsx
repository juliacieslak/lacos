import { HeartPulse, Info, Mail } from "lucide-react";
import { useState, type FormEventHandler } from "react";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import Button from "../components/ui/button";
import ErrorMessage from "../components/ui/errorMessage";
import Logo from "../components/ui/logo";
import AuthCard from "../layouts/authLayout";
import { useAuth } from "../hooks/useAuth";

// mensagens exibidas quando a sessão foi encerrada automaticamente.
const reasons: Record<string, string> = {
  inatividade: "Sua sessão foi encerrada após 15 minutos sem atividade. Entre novamente.",
  encerrada: "Sua sessão foi encerrada. Entre novamente.",
};

export default function LoginPage() {
  const { login, isLoading, error } = useAuth();
  const location = useLocation();
  const [params] = useSearchParams();
  // o e-mail não é autenticado pelo React: ele é enviado como login_hint para o Auth0 já abrir preenchido.
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const returnTo = (location.state as { from?: string } | null)?.from ?? "/dashboard";
  const reason = reasons[params.get("motivo") ?? ""];

  const handleSubmit: FormEventHandler<HTMLFormElement> = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    await login(returnTo, { email });
  };

  return (
    <AuthCard>
      <div className="flex justify-center">
        <Logo width={145} alt="Laços — cuidado conectado" />
      </div>

      <div className="mx-auto mt-4 flex w-fit items-center gap-2 rounded-full bg-lavender-100 px-4 py-2 text-xs font-medium text-lavender-700">
        <HeartPulse className="h-3.5 w-3.5" />
        <span>Cuidado conectado para famílias</span>
      </div>

      <div className="mt-8 text-center">
        <h1 className="font-display text-[30px] font-semibold tracking-[-0.02em] text-ink">Bem-vindo!</h1>
        <p className="mt-2 text-sm text-mist-500">Acesse sua conta para continuar</p>
      </div>

      {reason && (
        <p role="status" className="mt-6 flex items-start gap-2 rounded-xl bg-serene-50 px-4 py-3 text-sm text-serene-700">
          <Info className="mt-0.5 h-4 w-4 shrink-0" /> {reason}
        </p>
      )}
      {/* erros devolvidos pelo Auth0 (ex.: acesso negado, e-mail não verificado) */}
      <ErrorMessage error={error ? `Não foi possível entrar: ${error.message}` : null} className="mt-6" />

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <label className="block">
          <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-mist-500">E-mail</span>
          <span className="flex items-center gap-3 rounded-xl border border-mist-200 bg-mist-100 px-4 py-3.5 transition focus-within:border-lavender-500 focus-within:ring-2 focus-within:ring-lavender-300/50">
            <Mail className="h-4 w-4 text-mist-400" />
            <input
              type="email"
              value={email}
              autoComplete="email"
              onChange={(event) => setEmail(event.target.value)}
              placeholder="seu.email@exemplo.com"
              className="w-full bg-transparent text-sm text-ink outline-none placeholder:text-mist-400"
            />
          </span>
        </label>

        <Button type="submit" size="lg" fullWidth loading={submitting || isLoading}>Continuar</Button>
      </form>

      <div className="my-7 h-px bg-mist-200" />

      <p className="text-center text-sm text-mist-500">
        Ainda não tem conta?{" "}
        <Link to="/register" className="font-bold text-lavender-600 transition hover:text-lavender-700">Cadastre-se grátis</Link>
      </p>
      <p className="mt-5 text-center text-[11px] leading-4 text-mist-400">A autenticação é realizada de forma segura pelo Auth0.</p>
    </AuthCard>
  );
}
