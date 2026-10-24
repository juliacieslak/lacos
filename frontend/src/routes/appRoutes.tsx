// define as rotas do frontend: públicas (login/cadastro), protegidas (Auth0 + sessão no backend)
// e a área principal (exige cadastro concluído e consentimento LGPD vigente).
import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import MainLayout from "../components/layout/mainlayout";
import Loading from "../components/ui/loading";
import { PublicOnlyRoute, RequireAuth, RequireCompletedProfile } from "./guards";
import LoginPage from "../pages/loginPage";
import RegisterPage from "../pages/registerPage";

// páginas carregadas sob demanda (code splitting) para reduzir o tempo da primeira carga.
const OnboardingPage = lazy(() => import("../pages/onboardingPage"));
const ConsentPage = lazy(() => import("../pages/consentPage"));
const DashboardPage = lazy(() => import("../pages/dashboardPage"));
const PatientPage = lazy(() => import("../pages/patientPage"));
const PatientDetailsPage = lazy(() => import("../pages/patientDetailsPage"));
const PatientOverviewPage = lazy(() => import("../pages/patientOverviewPage"));
const MeasurementsPage = lazy(() => import("../pages/measurementsPage"));
const MedicationsPage = lazy(() => import("../pages/medicationsPage"));
const TasksPage = lazy(() => import("../pages/tasksPage"));
const AppointmentsPage = lazy(() => import("../pages/appointmentsPage"));
const HealthRecordsPage = lazy(() => import("../pages/healthRecordsPage"));
const DiaryPage = lazy(() => import("../pages/diaryPage"));
const DocumentsPage = lazy(() => import("../pages/documentsPage"));
const CareMembersPage = lazy(() => import("../pages/careMembersPage"));
const TimelinePage = lazy(() => import("../pages/timelinePage"));
const AgendaPage = lazy(() => import("../pages/agendaPage"));
const PatientPickerPage = lazy(() => import("../pages/patientPickerPage"));
const NearbyServicesPage = lazy(() => import("../pages/nearbyServicesPage"));
const InvitationsPage = lazy(() => import("../pages/invitationsPage"));
const NotificationsPage = lazy(() => import("../pages/notificationsPage"));
const ProfilePage = lazy(() => import("../pages/profilePage"));
const NotFoundPage = lazy(() => import("../pages/notFoundPage"));

export default function AppRoutes() {
  return (
    <Suspense fallback={<Loading fullScreen />}>
      <Routes>
        {/* públicas */}
        <Route element={<PublicOnlyRoute />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>

        {/* protegidas */}
        <Route element={<RequireAuth />}>
          <Route path="/onboarding" element={<OnboardingPage />} />
          <Route path="/consentimento" element={<ConsentPage />} />

          <Route element={<RequireCompletedProfile />}>
            <Route element={<MainLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/pacientes" element={<PatientPage />} />
              <Route path="/pacientes/:patientId" element={<PatientDetailsPage />}>
                <Route index element={<PatientOverviewPage />} />
                <Route path="medicoes" element={<MeasurementsPage />} />
                <Route path="medicamentos" element={<MedicationsPage />} />
                <Route path="rotina" element={<TasksPage />} />
                <Route path="compromissos" element={<AppointmentsPage />} />
                <Route path="saude" element={<HealthRecordsPage />} />
                <Route path="diario" element={<DiaryPage />} />
                <Route path="documentos" element={<DocumentsPage />} />
                <Route path="rede" element={<CareMembersPage />} />
                <Route path="historico" element={<TimelinePage />} />
              </Route>
              <Route path="/agenda" element={<AgendaPage />} />
              <Route path="/rotina" element={<PatientPickerPage tab="rotina" title="Rotina de cuidado" description="Selecione o paciente para ver e organizar a rotina." />} />
              <Route path="/rede" element={<PatientPickerPage tab="rede" title="Rede de apoio" description="Selecione o paciente para gerenciar quem participa do cuidado." />} />
              <Route path="/servicos-proximos" element={<NearbyServicesPage />} />
              <Route path="/convites" element={<InvitationsPage />} />
              <Route path="/notificacoes" element={<NotificationsPage />} />
              <Route path="/perfil" element={<ProfilePage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Route>
        </Route>

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Suspense>
  );
}
