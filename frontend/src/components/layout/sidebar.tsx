// menu lateral do Laços (itens do wireframe: Visão geral, Pacientes, Agenda, Rotina de cuidado, Rede de apoio, Serviços próximos).
import { CalendarDays, ClipboardCheck, HeartHandshake, LayoutDashboard, MapPin, ShieldCheck, Users, X } from "lucide-react";
import { NavLink } from "react-router-dom";
import Logo from "../ui/logo";
import { useSession } from "../../contexts/sessionContext";
import { profileTypeLabels } from "../../utils/labels";

const items = [
  { to: "/dashboard", label: "Visão geral", icon: LayoutDashboard },
  { to: "/pacientes", label: "Pacientes", icon: Users },
  { to: "/agenda", label: "Agenda", icon: CalendarDays },
  { to: "/rotina", label: "Rotina de cuidado", icon: ClipboardCheck },
  { to: "/rede", label: "Rede de apoio", icon: HeartHandshake },
  { to: "/servicos-proximos", label: "Serviços próximos", icon: MapPin },
];

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

export default function Sidebar({ open, onClose }: SidebarProps) {
  const { session } = useSession();
  const user = session?.user;

  return (
    <>
      {/* fundo escurecido no celular quando o menu está aberto */}
      <div className={`fixed inset-0 z-30 bg-ink/30 lg:hidden ${open ? "block" : "hidden"}`} onClick={onClose} aria-hidden />

      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-72 flex-col border-r border-mist-200 bg-white px-5 py-6 transition-transform lg:sticky lg:top-0 lg:h-screen lg:translate-x-0 ${open ? "translate-x-0" : "-translate-x-full"}`}
        aria-label="Menu principal"
      >
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Logo width={56} className="rounded-full" />
            <span className="font-display text-xl font-semibold text-ink">Laços</span>
          </div>
          <button type="button" onClick={onClose} className="rounded-lg p-1 text-mist-400 lg:hidden" aria-label="Fechar menu">
            <X className="h-5 w-5" />
          </button>
        </div>

        <p className="mt-8 px-3 text-xs font-medium uppercase tracking-[0.08em] text-mist-400">Espaço de cuidado</p>
        <nav className="mt-3 flex flex-col gap-1">
          {items.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              onClick={onClose}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                  isActive ? "bg-lavender-100 text-lavender-700" : "text-mist-500 hover:bg-mist-100 hover:text-ink"
                }`
              }
            >
              <Icon className="h-[18px] w-[18px]" aria-hidden />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto space-y-4">
          <div className="rounded-2xl border border-mist-200 bg-mist-50 p-4 text-center">
            <ShieldCheck className="mx-auto h-5 w-5 text-sage-700" aria-hidden />
            <p className="mt-2 text-sm font-semibold text-ink">Dados protegidos</p>
            <p className="mt-1 text-xs text-mist-500">Informações de saúde criptografadas e tratadas conforme a LGPD.</p>
          </div>
          {user && (
            <NavLink to="/perfil" onClick={onClose} className="flex items-center gap-3 rounded-xl p-2 hover:bg-mist-100">
              <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-lavender-200 font-semibold text-lavender-700">
                {user.name.charAt(0).toUpperCase()}
              </span>
              <span className="min-w-0">
                <span className="block truncate text-sm font-semibold text-ink">{user.name}</span>
                <span className="block truncate text-xs text-mist-500">{user.profileType ? profileTypeLabels[user.profileType] : "Perfil incompleto"}</span>
              </span>
            </NavLink>
          )}
        </div>
      </aside>
    </>
  );
}
