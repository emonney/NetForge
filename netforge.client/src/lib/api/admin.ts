import { api } from './client';

/** Permission strings the admin UI gates on — must match the backend slice constants
 * (Features/Roles/Permissions.cs, Features/Users/Permissions.cs). */
export const PERM = {
  usersRead: 'users.read',
  usersCreate: 'users.create',
  usersUpdate: 'users.update',
  usersDelete: 'users.delete',
  rolesRead: 'roles.read',
  rolesCreate: 'roles.create',
  rolesUpdate: 'roles.update',
  rolesDelete: 'roles.delete',
  settingsRead: 'settings.read',
  settingsUpdate: 'settings.update',
  auditRead: 'audit.read',
  webhooksRead: 'webhooks.read',
  webhooksCreate: 'webhooks.create',
  webhooksUpdate: 'webhooks.update',
  webhooksDelete: 'webhooks.delete',
} as const;

export interface Role {
  id: string;
  name: string;
  isSystem: boolean;
  permissions: string[];
  userCount: number;
}

export interface SaveRole {
  name: string;
  permissions: string[];
}

export interface CatalogPermission {
  name: string;
  description: string;
}

export interface PermissionGroup {
  name: string;
  permissions: CatalogPermission[];
}

export interface AdminUser {
  id: string;
  email: string;
  displayName: string | null;
  avatarUrl: string | null;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  lockedOut: boolean;
  roles: string[];
  createdAt: string;
  isSelf: boolean;
}

export const permissionsApi = {
  catalog: () => api.get<PermissionGroup[]>('/permissions'),
};

export const rolesApi = {
  list: () => api.get<Role[]>('/roles/'),
  create: (body: SaveRole) => api.post<Role>('/roles/', body),
  update: (id: string, body: SaveRole) => api.put<Role>(`/roles/${id}`, body),
  remove: (id: string) => api.del<void>(`/roles/${id}`),
};

export type SettingKind = 'boolean' | 'number' | 'string' | 'choice';
export type SettingValue = boolean | number | string;

export interface SettingOption {
  value: string;
  label: string;
}

export interface Setting {
  key: string;
  category: string;
  kind: SettingKind;
  value: SettingValue;
  defaultValue: SettingValue;
  /** Present for `choice` settings — render a dropdown. */
  options?: SettingOption[] | null;
}

export interface SettingCategory {
  category: string;
  settings: Setting[];
}

export const settingsApi = {
  list: () => api.get<SettingCategory[]>('/settings/'),
  update: (key: string, value: SettingValue) => api.put<void>(`/settings/${key}`, { value }),
};

/** Admin-provision a new user. Invite-first: omit `password` + set `sendInvite` to email a
 * "set your password" link; supply `password` to set a temporary one directly. */
export interface CreateUserBody {
  email: string;
  displayName?: string | null;
  roles?: string[];
  emailConfirmed: boolean;
  sendInvite: boolean;
  password?: string | null;
}

/** Admin edit of a user's basic identity. Changing the email re-requires confirmation. */
export interface UpdateUserBody {
  displayName?: string | null;
  email: string;
}

export const usersApi = {
  list: (search?: string) =>
    api.get<AdminUser[]>(`/users/${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  create: (body: CreateUserBody) => api.post<AdminUser>('/users/', body),
  update: (id: string, body: UpdateUserBody) => api.put<AdminUser>(`/users/${id}`, body),
  updateRoles: (id: string, roles: string[]) => api.put<AdminUser>(`/users/${id}/roles`, { roles }),
  confirmEmail: (id: string) => api.post<AdminUser>(`/users/${id}/confirm-email`),
  resendConfirmation: (id: string) => api.post<{ message: string }>(`/users/${id}/resend-confirmation`),
  sendPasswordReset: (id: string) => api.post<{ message: string }>(`/users/${id}/send-password-reset`),
  disableTwoFactor: (id: string) => api.post<AdminUser>(`/users/${id}/disable-2fa`),
  lock: (id: string) => api.post<AdminUser>(`/users/${id}/lock`),
  unlock: (id: string) => api.post<AdminUser>(`/users/${id}/unlock`),
  remove: (id: string) => api.del<void>(`/users/${id}`),
};
