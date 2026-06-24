import { api } from './client';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string | null;
  avatarUrl: string | null;
  locale: string | null;
  timeZone: string | null;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  hasPassword: boolean;
  roles: string[];
  permissions: string[];
}

export interface LoginResult {
  requiresTwoFactor: boolean;
  user: AuthUser | null;
}

export interface OAuthProvider {
  name: string;
  displayName: string;
}

/** Anonymous login/register screen config (no auth required). */
export interface PublicConfig {
  allowRegistration: boolean;
  demoLogin: { email: string; password: string } | null;
}

export interface ActiveSession {
  id: string;
  deviceName: string | null;
  ipAddress: string | null;
  createdAt: string;
  lastSeenAt: string;
  current: boolean;
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface LinkedLogin {
  provider: string;
  displayName: string;
}

/** Typed wrappers over the Features/Auth endpoints. Replaced by a generated client later. */
export const authApi = {
  me: () => api.get<AuthUser>('/auth/me'),
  login: (body: { email: string; password: string; rememberMe?: boolean }) =>
    api.post<LoginResult>('/auth/login', body),
  loginTwoFactor: (body: { code: string; rememberMachine?: boolean; rememberMe?: boolean }) =>
    api.post<LoginResult>('/auth/login/2fa', body),
  loginRecoveryCode: (body: { recoveryCode: string; rememberMe?: boolean }) =>
    api.post<LoginResult>('/auth/login/recovery-code', body),
  register: (body: { email: string; password: string; displayName?: string }) =>
    api.post<{ message: string }>('/auth/register', body),
  confirmEmail: (body: { userId: string; token: string }) =>
    api.post<{ message: string }>('/auth/confirm-email', body),
  resendConfirmation: (body: { email: string }) =>
    api.post<{ message: string }>('/auth/resend-confirmation', body),
  forgotPassword: (body: { email: string }) =>
    api.post<{ message: string }>('/auth/forgot-password', body),
  resetPassword: (body: { email: string; token: string; newPassword: string }) =>
    api.post<{ message: string }>('/auth/reset-password', body),
  changePassword: (body: { currentPassword?: string; newPassword: string }) =>
    api.post<{ message: string }>('/auth/change-password', body),
  updateProfile: (body: { displayName: string | null }) => api.put<AuthUser>('/auth/profile', body),
  updatePreferences: (body: { locale?: string | null; timeZone?: string | null }) =>
    api.put<AuthUser>('/auth/preferences', body),
  logout: () => api.post<void>('/auth/logout'),

  publicConfig: () => api.get<PublicConfig>('/auth/public-config'),

  providers: () => api.get<OAuthProvider[]>('/auth/external/providers'),

  sessions: () => api.get<ActiveSession[]>('/auth/sessions/'),
  revokeSession: (id: string) => api.post<{ message: string; current: boolean }>(`/auth/sessions/${id}/revoke`),
  revokeOtherSessions: () => api.post<{ message: string; count: number }>('/auth/sessions/revoke-others'),

  twoFactorSetup: () => api.post<TwoFactorSetup>('/auth/2fa/setup'),
  twoFactorEnable: (body: { code: string }) => api.post<{ recoveryCodes: string[] }>('/auth/2fa/enable', body),
  twoFactorDisable: () => api.post<{ message: string }>('/auth/2fa/disable'),
  twoFactorRegenerateCodes: () => api.post<{ recoveryCodes: string[] }>('/auth/2fa/recovery-codes'),

  links: () => api.get<LinkedLogin[]>('/auth/external/links'),
  unlink: (provider: string) => api.post<{ message: string }>(`/auth/external/${provider}/unlink`),
};
