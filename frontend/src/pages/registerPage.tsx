// cadastro: a conta (e-mail e senha) é criada na tela segura do Auth0.
// os campos de senha da versão anterior foram removidos porque nunca eram enviados a lugar algum e
// incentivavam digitar a senha fora do provedor de identidade. Os demais dados do RF01 são pedidos no onboarding.
import { LockKeyhole, Mail, Sparkles } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import Button from "../components/ui/button";
import Logo from "../components/ui/logo";
import AuthCard from "../layouts/authLayout";
import { useAuth } from "../hooks/useAuth";

export default function RegisterPage() {
  const { login, isLoading } = useAuth();
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    // abre diretamente a tela de cadastro do Auth0; ao voltar, o onboarding completa o perfil.
    await login("/onboarding", { email, signup: true });
  };

  return (
    <AuthCard>
      <div className="flex justify-center">
        <Logo width={145} alt="Laços — cuidado conectado" />
      </div>
      <div className="mx-auto mt-4 flex w-fit items-center gap-2 rounded-full bg-lavender-100 px-4 py-2 text-xs font-medium text-lavender-700">
        <Sparkles className="h-3.5 w-3.5" />
        <span>Comece sua jornada de cuidado</span>
      </div>
      <div className="mt-8 text-center">
        <h1 className="font-display text-[30px] font-semibold tracking-[-0.02em] text-ink">Crie sua conta</h1>
        <p className="mt-2 text-sm text-mist-500">Organize o cuidado em um só lugar</p>
      </div>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <label className="block">
          <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-mist-500">E-mail</span>
          <span className="flex items-center gap-3 rounded-xl border border-mist-200 bg-mist-100 px-4 py-3.5 transition focus-within:border-lavender-500 focus-within:ring-2 focus-within:ring-lavender-300/50">
            <Mail className="h-4 w-4 shrink-0 text-mist-400" />
            <input
              type="email"
              value={email}
              autoComplete="email"
              onChange={(e) => setEmail(e.target.value)}
              placeholder="seu.email@exemplo.com"
              className="w-full bg-transparent text-sm text-ink outline-none placeholder:text-mist-400"
            />
          </span>
        </label>

        <p className="flex items-start gap-2 rounded-xl bg-lavender-50 px-4 py-3 text-xs leading-5 text-mist-700">
          <LockKeyhole className="mt-0.5 h-4 w-4 shrink-0 text-lavender-600" />
          Você criará sua senha na página segura do Auth0. Em seguida, pediremos os dados do seu perfil (nome, CPF, telefone…).
        </p>

        <Button type="submit" size="lg" fullWidth loading={submitting || isLoading}>Criar minha conta</Button>
      </form>

      <div className="my-7 h-px bg-mist-200" />
      <p className="text-center text-sm text-mist-500">
        Já possui uma conta?{" "}
        <Link to="/login" className="font-bold text-lavender-600 transition hover:text-lavender-700">Acesse sua conta</Link>
      </p>
      <p className="mt-5 text-center text-[11px] leading-4 text-mist-400">A autenticação é realizada de forma segura pelo Auth0.</p>
    </AuthCard>
  );
}
