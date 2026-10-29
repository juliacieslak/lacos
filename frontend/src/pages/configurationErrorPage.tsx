// exibida quando as variáveis do Auth0 não foram configuradas (a aplicação inicia mesmo assim).
import { Settings } from "lucide-react";

export default function ConfigurationErrorPage({ missing }: { missing: string[] }) {
  return (
    <main className="grid min-h-screen place-items-center bg-mist-50 px-4">
      <section className="w-full max-w-lg rounded-[var(--radius-card)] border border-mist-200 bg-white p-8 shadow-[var(--shadow-soft)]">
        <Settings className="h-8 w-8 text-lavender-600" aria-hidden />
        <h1 className="mt-4 font-display text-xl font-semibold text-ink">Configuração de autenticação pendente</h1>
        <p className="mt-2 text-sm text-mist-500">
          Crie o arquivo <code className="rounded bg-mist-100 px-1">frontend/.env.local</code> (a partir do <code className="rounded bg-mist-100 px-1">.env.example</code>) e preencha:
        </p>
        <ul className="mt-3 list-inside list-disc text-sm text-ink">
          {missing.map((name) => <li key={name}><code>{name}</code></li>)}
        </ul>
        <p className="mt-4 text-xs text-mist-500">Depois reinicie o servidor do Vite (npm run dev).</p>
      </section>
    </main>
  );
}
