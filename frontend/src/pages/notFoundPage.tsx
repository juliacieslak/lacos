// rota inexistente.
import { Link } from "react-router-dom";
import Button from "../components/ui/button";
import EmptyState from "../components/ui/emptyState";

export default function NotFoundPage() {
  return (
    <div className="py-10">
      <EmptyState title="Página não encontrada" description="O endereço acessado não existe ou foi movido."
        action={<Link to="/dashboard"><Button>Voltar ao início</Button></Link>} />
    </div>
  );
}
