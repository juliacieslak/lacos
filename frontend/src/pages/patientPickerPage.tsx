// atalhos do menu ("Rotina de cuidado" e "Rede de apoio"): abre a aba correspondente do paciente.
// com um único paciente, redireciona direto; com vários, pede para escolher.
import { ChevronRight } from "lucide-react";
import { Link, Navigate } from "react-router-dom";
import { PageHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import { usePatients } from "../hooks/usePatients";

export default function PatientPickerPage({ tab, title, description }: { tab: string; title: string; description: string }) {
  const { data, loading, error, reload } = usePatients();
  if (loading && !data) return <Loading />;
  if (error) return <ErrorMessage error={error} onRetry={reload} />;
  if (data?.length === 1) return <Navigate to={`/pacientes/${data[0].id}/${tab}`} replace />;
  return (
    <>
      <PageHeader eyebrow={title} title="Escolha o paciente" description={description} />
      {!data?.length ? <EmptyState title="Nenhum paciente" description="Cadastre um paciente ou aceite um convite." /> : (
        <ul className="space-y-2">
          {data.map((p) => (
            <li key={p.id}>
              <Link to={`/pacientes/${p.id}/${tab}`} className="flex items-center gap-3 rounded-2xl border border-mist-200 bg-white p-4 hover:border-lavender-300">
                <span className="grid h-10 w-10 place-items-center rounded-full bg-lavender-200 font-semibold text-lavender-700">{p.fullName.charAt(0)}</span>
                <span className="flex-1 font-semibold text-ink">{p.fullName}</span>
                <ChevronRight className="h-4 w-4 text-mist-400" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
