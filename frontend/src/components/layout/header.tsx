// cabeçalho superior: data, saudação, notificações e menu da conta.
import { Bell, ChevronDown, LogOut, Mail, Menu, UserRound } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useSession } from "../../contexts/sessionContext";
import { useAuth } from "../../hooks/useAuth";
import { notificationService } from "../../services/notificationService";
import { formatLongDate, greeting } from "../../utils/formatDate";

export default function Header({ onOpenMenu }: { onOpenMenu: () => void }) {
  const { session } = useSession();
  const { logout } = useAuth();
  const [unread, setUnread] = useState(0);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const firstName = session?.user.name.split(" ")[0] ?? "";

  // contador de notificações não lidas, atualizado a cada minuto (RNF11: alertas em até 1 minuto).
  useEffect(() => {
    const controller = new AbortController();
    const load = () =>
      notificationService
        .list(true, 1, controller.signal)
        .then((r) => setUnread(r.unreadCount))
        .catch(() => undefined);
    load();
    const timer = window.setInterval(load, 60_000);
    window.addEventListener("lacos:notifications-changed", load);
    return () => {
      controller.abort();
      window.clearInterval(timer);
      window.removeEventListener("lacos:notifications-changed", load);
    };
  }, []);

  useEffect(() => {
    const close = (e: MouseEvent) => menuRef.current && !menuRef.current.contains(e.target as Node) && setMenuOpen(false);
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  return (
    <header className="sticky top-0 z-20 flex items-center justify-between gap-3 border-b border-mist-200 bg-white/90 px-4 py-3 backdrop-blur sm:px-8">
      <div className="flex items-center gap-3">
        <button type="button" onClick={onOpenMenu} className="rounded-lg p-2 text-mist-500 hover:bg-mist-100 lg:hidden" aria-label="Abrir menu">
          <Menu className="h-5 w-5" />
        </button>
        <div>
          <p className="text-xs text-mist-500">{formatLongDate(new Date())}</p>
          <p className="text-sm font-semibold text-ink">{greeting()}, {firstName}.</p>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <Link to="/notificacoes" className="relative rounded-xl p-2.5 text-mist-500 hover:bg-lavender-50 hover:text-lavender-700" aria-label={`Notificações (${unread} não lidas)`}>
          <Bell className="h-5 w-5" />
          {unread > 0 && (
            <span className="absolute right-1 top-1 grid h-4 min-w-4 place-items-center rounded-full bg-danger-600 px-1 text-[10px] font-bold text-white">
              {unread > 99 ? "99+" : unread}
            </span>
          )}
        </Link>

        <div className="relative" ref={menuRef}>
          <button type="button" onClick={() => setMenuOpen((v) => !v)} className="flex items-center gap-2 rounded-xl p-1.5 hover:bg-mist-100" aria-expanded={menuOpen} aria-haspopup="menu">
            <span className="grid h-8 w-8 place-items-center rounded-full bg-lavender-200 text-sm font-semibold text-lavender-700">{firstName.charAt(0).toUpperCase()}</span>
            <span className="hidden text-sm font-medium text-ink sm:block">{firstName}</span>
            <ChevronDown className="hidden h-4 w-4 text-mist-400 sm:block" />
          </button>
          {menuOpen && (
            <div role="menu" className="absolute right-0 mt-2 w-52 overflow-hidden rounded-xl border border-mist-200 bg-white py-1 shadow-[var(--shadow-card)]">
              <Link role="menuitem" to="/perfil" onClick={() => setMenuOpen(false)} className="flex items-center gap-2 px-4 py-2.5 text-sm hover:bg-mist-100"><UserRound className="h-4 w-4" /> Meu perfil</Link>
              <Link role="menuitem" to="/convites" onClick={() => setMenuOpen(false)} className="flex items-center gap-2 px-4 py-2.5 text-sm hover:bg-mist-100"><Mail className="h-4 w-4" /> Convites</Link>
              <button role="menuitem" type="button" onClick={() => void logout()} className="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-danger-600 hover:bg-danger-50"><LogOut className="h-4 w-4" /> Sair</button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
