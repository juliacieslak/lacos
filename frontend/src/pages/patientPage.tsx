// lista todos os pacientes vinculados ao usuário (WF02): busca, filtro de status e cadastro.
import { Plus, Search } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import PatientForm from "../components/patients/patientForm";
import PatientList from "../components/patients/patientList";
import Button from "../components/ui/button";
import { PageHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Modal from "../components/ui/modal";
import Select from "../components/ui/select";
import { useToast } from "../contexts/toastContext";
import { usePatients } from "../hooks/usePatients";
import { patientService } from "../services/patientService";
import type { CreatePatientRequest, Patient, PatientCareStatus } from "../types/patient";
import { careStatusLabels, toOptions } from "../utils/labels";

export default function PatientPage() {
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();
  const toast = useToast();
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<PatientCareStatus | "">("");
  const creating = params.get("novo") === "1";
  const { data, loading, error, reload } = usePatients(search, status || undefined);

  // espera o usuário parar de digitar (300 ms) antes de consultar a API.
  useEffect(() => {
    const t = window.setTimeout(() => setSearch(searchInput), 300);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  const create = async (data: CreatePatientRequest) => {
    const patient: Patient = await patientService.create(data);
    toast.success(`Paciente ${patient.fullName} cadastrado (${patient.publicCode}).`);
    navigate(`/pacientes/${patient.id}`);
  };

  return (
    <>
      <PageHeader eyebrow="Pacientes" title="Meus pacientes" description="Acompanhe e gerencie os pacientes que você cuida."
        action={<Button icon={<Plus className="h-4 w-4" />} onClick={() => setParams({ novo: "1" })}>Cadastrar paciente</Button>} />

      <div className="mb-5 flex flex-col gap-3 sm:flex-row">
        <label className="flex flex-1 items-center gap-3 rounded-xl border border-mist-200 bg-white px-4 py-2.5 focus-within:border-lavender-500 focus-within:ring-2 focus-within:ring-lavender-300/50">
          <Search className="h-4 w-4 text-mist-400" />
          <input value={searchInput} onChange={(e) => setSearchInput(e.target.value)} placeholder="Buscar por nome do paciente"
            aria-label="Buscar por nome do paciente" className="w-full bg-transparent text-sm outline-none placeholder:text-mist-400" />
        </label>
        <Select ariaLabel="Filtrar por status" value={status} onChange={(e) => setStatus(e.target.value as PatientCareStatus | "")}
          options={[{ value: "", label: "Todos os status" }, ...toOptions(careStatusLabels)]} />
      </div>

      {loading && !data ? <Loading /> : error ? <ErrorMessage error={error} onRetry={reload} /> : (
        <PatientList patients={data ?? []} filtered={Boolean(search || status)} />
      )}

      <Modal open={creating} title="Cadastrar novo paciente" size="lg" onClose={() => setParams({})}>
        <PatientForm onSubmit={(d) => create(d as CreatePatientRequest)} onCancel={() => setParams({})} />
      </Modal>
    </>
  );
}
