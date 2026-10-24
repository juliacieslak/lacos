// notificações do usuário e preferências por paciente (RF12).
import { api } from "./api";
import type { NotificationList, NotificationPreference, NotificationPreferenceRequest } from "../types/notification";

export const notificationService = {
  list: (unreadOnly = false, page = 1, signal?: AbortSignal) =>
    api.get<NotificationList>("/api/notifications", { unreadOnly, page, pageSize: 20 }, signal),
  markRead: (id: string) => api.post<void>(`/api/notifications/${id}/read`),
  markAllRead: () => api.post<{ updated: number }>("/api/notifications/read-all"),
  preferences: (signal?: AbortSignal) => api.get<NotificationPreference[]>("/api/notifications/preferences", undefined, signal),
  updatePreference: (patientId: string, data: NotificationPreferenceRequest) =>
    api.put<NotificationPreference>(`/api/notifications/preferences/${patientId}`, data),
};
