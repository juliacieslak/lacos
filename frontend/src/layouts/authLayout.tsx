// moldura visual das telas públicas (login/cadastro) e do onboarding — preservada do frontend original.
import type { ReactNode } from "react";

export default function AuthCard({ children, wide = false }: { children: ReactNode; wide?: boolean }) {
  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-mist-50 px-4 py-10 font-sans">
      {/* decoração do fundo */}
      <div className="pointer-events-none absolute -left-32 top-16 h-72 w-72 rounded-full bg-lavender-300/25 blur-3xl" />
      <div className="pointer-events-none absolute -right-32 bottom-10 h-80 w-80 rounded-full bg-sage-300/25 blur-3xl" />
      <section className={`relative w-full rounded-[28px] border border-mist-200 bg-white px-7 py-9 shadow-[var(--shadow-soft)] sm:px-11 sm:py-10 ${wide ? "max-w-2xl" : "max-w-[470px]"}`}>
        {children}
      </section>
    </main>
  );
}
