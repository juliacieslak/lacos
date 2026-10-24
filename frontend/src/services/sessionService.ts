// sessão e conta do usuário (POST /api/auth/session, /api/users/me...).
import { api } from "./api";
import type { CompleteOnboardingRequest, ConsentStatus, ConsentType, ProfileFields, Session, User } from "../types/user";

export const sessionService = {
  // registra o usuário no primeiro acesso e devolve a situação de acesso.
  start: () => api.post<Session>("/api/auth/session"),
  // revoga o token atual no backend antes do logout do Auth0.
  logout: () => api.post<void>("/api/auth/logout"),
  me: () => api.get<User>("/api/users/me"),
  completeOnboarding: (data: CompleteOnboardingRequest) => api.post<User>("/api/users/me/onboarding", data),
  updateProfile: (data: ProfileFields) => api.put<User>("/api/users/me", data),
  consents: () => api.get<ConsentStatus>("/api/users/me/consents"),
  acceptConsents: (acceptTermsOfUse: boolean, acceptSensitiveDataProcessing: boolean) =>
    api.post<ConsentStatus>("/api/users/me/consents", { acceptTermsOfUse, acceptSensitiveDataProcessing }),
  revokeConsent: (consentType: ConsentType) => api.post<ConsentStatus>("/api/users/me/consents/revoke", { consentType }),
  deleteAccount: (confirmation: string, reason: string | null) => api.delete("/api/users/me", { confirmation, reason }),
};
