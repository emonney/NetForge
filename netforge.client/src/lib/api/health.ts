import { api } from './client';

/** Permission gating the health dashboard — matches Features/Health/Permissions.cs. */
export const HEALTH_PERM = { read: 'health.read' } as const;

export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface HealthEntry {
  name: string;
  status: HealthStatus;
  description: string | null;
  durationMs: number;
  tags: string[];
  error: string | null;
  data: Record<string, string>;
}

export interface HealthReport {
  status: HealthStatus;
  totalDurationMs: number;
  checkedAt: string;
  checks: HealthEntry[];
}

export const healthApi = {
  get: () => api.get<HealthReport>('/health/'),
};
