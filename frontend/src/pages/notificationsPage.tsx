// notificações recebidas e preferências por paciente (RF12 / UC10 "Configurar Notificações").
import { Bell, CheckCheck } from "lucide-react";
import { useState } from "react";
import Badge from "../components/ui/badge";
import Button from "../components/ui/button";
import { Card, CardHeader, PageHeader } from "../components/ui/card";
import EmptyState from "../components/ui/emptyState";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import Pagination from "../components/ui/pagination";
import { useToast } from "../contexts/toastContext";
import { toApiError, useAsync } from "../hooks/useAsync";
import { notificationService } from "../services/notificationService";
import type { NotificationPreference, NotificationPreferenceRequest } from "../types/notification";
import { formatDateTime } from "../utils/formatDate";
import { notificationCategoryLabels } from "../utils/labels";

const prefFields: [keyof NotificationPreferenceRequest, string][] = [
  ["medicationReminder", notificationCategoryLabels.MEDICATION_REMINDER],
  ["medicationMissed", notificationCategoryLabels.MEDICATION_MISSED],
  ["appointmentReminder", notificationCategoryLabels.APPOINTMENT_REMINDER],
  ["careRoutineCheckin", notificationCategoryLabels.CARE_ROUTINE_CHECKIN],
  ["patientDataAdded", notificationCategoryLabels.PATIENT_DATA_ADDED],
  ["healthThresholdAlert", notificationCategoryLabels.HEALTH_THRESHOLD_ALERT],
];

const notifyHeader = () => window.dispatchEvent(new Event("lacos:notifications-changed"));

export default function NotificationsPage() {
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const list = useAsync((s) => notificationService.list(unreadOnly, page, s), [unreadOnly, page]);
  const prefs = useAsync((s) => notificationService.preferences(s), []);

  const markRead = async (id: string) => {
    await notificationService.markRead(id).catch(() => undefined);
    list.reload();
    notifyHeader();
  };
  const markAll = async () => {
    await notificationService.markAllRead();
    list.reload();
    notifyHeader();
  };

  const updatePref = async (pref: NotificationPreference, key: keyof NotificationPreferenceRequest, value: boolean) => {
    const { patientId, patientName, ...current } = pref;
    void patientName;
    try {
      const saved = await notificationService.updatePreference(patientId, { ...current, [key]: value });
      prefs.setData((all) => (all ?? []).map((p) => (p.patientId === patientId ? saved : p)));
    } catch (e) {
      toast.error(toApiError(e).message);
    }
  };

  return (
    <>
      <PageHeader eyebrow="Notificações" title="Avisos e lembretes" />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
        <Card>
          <CardHeader title="Recebidas" subtitle={list.data ? `${list.data.unreadCount} não lida(s)` : undefined} icon={<Bell className="h-4 w-4" />}
            action={
              <div className="flex flex-wrap gap-2">
                <label className="flex items-center gap-2 text-xs text-mist-500"><input type="checkbox" className="accent-lavender-600" checked={unreadOnly} onChange={(e) => { setUnreadOnly(e.target.checked); setPage(1); }} /> Só não lidas</label>
                <Button size="sm" variant="secondary" icon={<CheckCheck className="h-3.5 w-3.5" />} onClick={() => void markAll()}>Marcar todas como lidas</Button>
              </div>
            } />
          {list.loading && !list.data ? <Loading /> : list.error ? <ErrorMessage error={list.error} onRetry={list.reload} /> : !list.data?.page.items.length ? (
            <EmptyState title="Nenhuma notificação" />
          ) : (
            <>
              <ul className="divide-y divide-mist-100">
                {list.data.page.items.map((n) => (
                  <li key={n.id} className={`flex items-start gap-3 py-3 ${n.readAt ? "opacity-70" : ""}`}>
                    <span className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${n.readAt ? "bg-mist-300" : "bg-lavender-500"}`} aria-hidden />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-semibold text-ink">{n.title}</p>
                      <p className="text-sm text-mist-700">{n.message}</p>
                      <div className="mt-1 flex flex-wrap items-center gap-2">
                        <Badge tone={n.category === "HEALTH_THRESHOLD_ALERT" || n.category === "MEDICATION_MISSED" ? "danger" : "lavender"}>{notificationCategoryLabels[n.category]}</Badge>
                        <span className="text-[11px] text-mist-400">{formatDateTime(n.createdAt)}</span>
                      </div>
                    </div>
                    {!n.readAt && <button type="button" onClick={() => void markRead(n.id)} className="text-xs font-semibold text-lavender-600 hover:underline">Marcar como lida</button>}
                  </li>
                ))}
              </ul>
              <Pagination page={list.data.page.page} totalPages={list.data.page.totalPages} onChange={setPage} />
            </>
          )}
        </Card>

        <Card>
          <CardHeader title="Preferências" subtitle="Escolha o que deseja receber de cada paciente" />
          {prefs.loading && !prefs.data ? <Loading /> : prefs.error ? <ErrorMessage error={prefs.error} onRetry={prefs.reload} /> : !prefs.data?.length ? (
            <p className="text-sm text-mist-500">Você ainda não acompanha pacientes.</p>
          ) : (
            <div className="space-y-5">
              {prefs.data.map((pref) => (
                <fieldset key={pref.patientId}>
                  <legend className="mb-2 flex w-full items-center justify-between text-sm font-semibold text-ink">
                    {pref.patientName}
                    <label className="flex items-center gap-1.5 text-xs font-normal text-mist-500">
                      <input type="checkbox" className="accent-lavender-600" checked={pref.isActive} onChange={(e) => void updatePref(pref, "isActive", e.target.checked)} /> Ativas
                    </label>
                  </legend>
                  <div className="space-y-1.5">
                    {prefFields.map(([key, label]) => (
                      <label key={key} className={`flex items-center gap-2 text-sm ${pref.isActive ? "text-mist-700" : "text-mist-400"}`}>
                        <input type="checkbox" className="h-4 w-4 accent-lavender-600" disabled={!pref.isActive} checked={pref[key]} onChange={(e) => void updatePref(pref, key, e.target.checked)} />
                        {label}
                      </label>
                    ))}
                  </div>
                </fieldset>
              ))}
            </div>
          )}
        </Card>
      </div>
    </>
  );
}
