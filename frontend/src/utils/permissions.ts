// classifica as permissões granulares (RF07) nos perfis exibidos ao usuário (US01).
import type { Permissions } from "../types/common";

export function permissionLevel(p: Permissions) {
  if (p.canManageMembers) return { label: "Administrador", tone: "lavender" as const };
  if (p.canAdd && p.canEdit && p.canDelete) return { label: "Edição", tone: "sage" as const };
  if (!p.canAdd && !p.canEdit && !p.canDelete) return { label: "Apenas leitura", tone: "serene" as const };
  return { label: "Personalizado", tone: "neutral" as const };
}
