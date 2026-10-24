// detalhes completos de um paciente, organizados em abas (resumo, medições, medicamentos, rotina, agenda, saúde,
// diário, documentos, rede de apoio e histórico). Cada aba é uma rota filha que recebe o paciente pelo Outlet.
import { ArrowLeft } from "lucide-react";
import { Link, NavLink, Outlet, useParams } from "react-router-dom";
import PatientHeader from "../components/patients/patientHeader";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import type { PatientOutletContext } from "../contexts/patientContext";
import { usePatient } from "../hooks/usePatients";

const tabs = [
  { to: "", label: "Resumo", end: true },
  { to: "medicoes", label: "Medições" },
  { to: "medicamentos", label: "Medicamentos" },
  { to: "rotina", label: "Rotina" },
  { to: "compromissos", label: "Compromissos" },
  { to: "saude", label: "Saúde" },
  { to: "diario", label: "Diário" },
  { to: "documentos", label: "Documentos" },
  { to: "rede", label: "Rede de apoio" },
  { to: "historico", label: "Histórico" },
];

export default function PatientDetailsPage() {
  const { patientId } = useParams();
  const { data: patient, loading, error, reload } = usePatient(patientId);

  if (loading && !patient) return <Loading label="Carregando paciente..." />;
  if (error || !patient)
    return (
      <div className="space-y-4">
        <Link to="/pacientes" className="inline-flex items-center gap-1 text-sm font-semibold text-lavender-600"><ArrowLeft className="h-4 w-4" /> Pacientes</Link>
        <ErrorMessage error={error?.status === 404 ? "Paciente não encontrado ou você não tem mais acesso a ele." : error} onRetry={error?.status === 404 ? undefined : reload} />
      </div>
    );

  const context: PatientOutletContext = { patient, reloadPatient: reload };

  return (
    <>
      <Link to="/pacientes" className="mb-4 inline-flex items-center gap-1 text-sm font-semibold text-lavender-600"><ArrowLeft className="h-4 w-4" /> Pacientes</Link>
      <PatientHeader patient={patient} />
      <nav className="-mx-4 mt-6 overflow-x-auto px-4 sm:mx-0 sm:px-0" aria-label="Seções do paciente">
        <ul className="flex min-w-max gap-1 border-b border-mist-200">
          {tabs.map((t) => (
            <li key={t.label}>
              <NavLink to={t.to} end={t.end}
                className={({ isActive }) => `block border-b-2 px-3 py-2.5 text-sm font-medium transition ${
                  isActive ? "border-lavender-600 text-lavender-700" : "border-transparent text-mist-500 hover:text-ink"}`}>
                {t.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <div className="mt-6">
        <Outlet context={context} />
      </div>
    </>
  );
}
