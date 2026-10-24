// mostra visualmente o nível de acesso do membro (Administrador, Edição, Leitura, Personalizado).
import Badge from "../ui/badge";
import type { Permissions } from "../../types/common";
import { permissionLevel } from "../../utils/permissions";

export default function PermissionBadge({ permissions }: { permissions: Permissions }) {
  const level = permissionLevel(permissions);
  return <Badge tone={level.tone}>{level.label}</Badge>;
}
