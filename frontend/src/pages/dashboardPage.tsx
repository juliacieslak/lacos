// tela principal (WF01): pacientes, agenda de hoje, últimas medições e rede de apoio.
import { Compass, MailOpen, Plus, Search } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import CareMembersCard from "../components/dashboard/careMembersCard";
import HealthSummaryCards from "../components/dashboard/healthSummaryCards";
import PatientSummary from "../components/dashboard/patientSummary";
import TodayTasks from "../components/dashboard/todayTasks";
import Button from "../components/ui/button";
import { Card, PageHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Select from "../components/ui/select";
import { useSession } from "../contexts/sessionContext";
import { useAsync } from "../hooks/useAsync";
import { dashboardService } from "../services/dashboardService";

export default function DashboardPage() {
  const { session } = useSession();
  const [patientId, setPatientId] = useState<string>("");
  const { data, loading, error, reload } = useAsync((signal) => dashboardService.get(patientId || undefined, signal), [patientId]);

  if (loading && !data) return <Loading label="Carregando visão geral..." />;
  if (error && !data) return <ErrorMessage error={error} onRetry={reload} />;
  if (!data) return null;

  const selected = data.patients.find((p) => p.id === data.selectedPatientId);
  const pendingInvites = session?.pendingInvitations.length ?? 0;

  return (
    <>
      <PageHeader eyebrow="Visão geral" title="Seu espaço de cuidado" description="Acompanhe os cuidados, medicações e compromissos do dia."
        action={data.patients.length > 1 ? (
          <Select ariaLabel="Paciente exibido" value={patientId} onChange={(e) => setPatientId(e.target.value)}
            options={[{ value: "", label: "Todos os pacientes" }, ...data.patients.map((p) => ({ value: p.id, label: p.fullName }))]} />
        ) : undefined} />

      {pendingInvites > 0 && (
        <Link to="/convites" className="mb-6 flex items-center gap-3 rounded-2xl border border-lavender-200 bg-lavender-50 p-4 text-sm text-lavender-700">
          <MailOpen className="h-5 w-5" /> Você tem {pendingInvites} convite(s) para participar de uma rede de cuidado. <span className="font-semibold">Ver convites →</span>
        </Link>
      )}

      {data.patients.length === 0 ? (
        <EmptyState
          icon={<Compass className="h-5 w-5" />}
          title="Nenhum paciente por aqui ainda"
          description="Cadastre um paciente para começar a organizar seus cuidados, agenda e rede de apoio — ou aguarde o convite de um administrador."
          action={<Link to="/pacientes?novo=1"><Button icon={<Plus className="h-4 w-4" />}>Cadastrar paciente</Button></Link>}
        />
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
          <div className="space-y-6">
            {selected ? <PatientSummary patient={selected} /> : (
              <div className="space-y-3">{data.patients.map((p) => <PatientSummary key={p.id} patient={p} />)}</div>
            )}
            <TodayTasks items={data.todayAgenda} pending={data.todayPending} completed={data.todayCompleted} patients={data.patients} onChanged={reload} />
          </div>
          <div className="space-y-6">
            {selected && (
              <Card>
                <h2 className="mb-3 font-display text-base font-semibold text-ink">Últimas medições</h2>
                <HealthSummaryCards metrics={data.latestMetrics} />
                <Link to={`/pacientes/${selected.id}/medicoes`} className="mt-3 inline-block text-sm font-semibold text-lavender-600">Ver histórico →</Link>
              </Card>
            )}
            {selected && data.careTeam.length > 0 && (
              <CareMembersCard members={data.careTeam} patientId={selected.id} canManage={selected.myPermissions.canManageMembers} />
            )}
            <Card className="bg-gradient-to-br from-lavender-50 to-serene-50">
              <p className="font-display font-semibold text-ink">Tudo em um só lugar</p>
              <p className="mt-1 text-sm text-mist-500">Consulte o histórico completo, documentos e serviços de saúde próximos.</p>
              <div className="mt-4 flex flex-wrap gap-2">
                {selected && <Link to={`/pacientes/${selected.id}/historico`}><Button size="sm" variant="secondary">Ver histórico</Button></Link>}
                <Link to="/servicos-proximos"><Button size="sm" variant="secondary" icon={<Search className="h-3.5 w-3.5" />}>Farmácias próximas</Button></Link>
              </div>
            </Card>
          </div>
        </div>
      )}
    </>
  );
}
