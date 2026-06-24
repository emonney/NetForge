import { ApiError, type ProblemDetails } from '@/lib/problem';

const BASE = '/api';

type Method = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

interface RequestOptions {
  /** Abort the in-flight request — used to cancel stale reads (e.g. type-ahead search). */
  signal?: AbortSignal;
}

async function request<T>(method: Method, path: string, body?: unknown, options?: RequestOptions): Promise<T> {
  const response = await fetch(`${BASE}${path}`, {
    method,
    // Cookie auth: always send credentials so the auth cookie rides along.
    credentials: 'include',
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal: options?.signal,
  });

  if (response.status === 204) return undefined as T;

  const isJson = response.headers.get('content-type')?.includes('json') ?? false;
  const data = isJson ? await response.json().catch(() => null) : null;

  if (!response.ok) {
    const problem: ProblemDetails =
      data && typeof data === 'object' && 'status' in data
        ? (data as ProblemDetails)
        : { status: response.status, title: response.statusText };
    throw new ApiError(problem);
  }

  return data as T;
}

export const api = {
  get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, undefined, options),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body),
  del: <T>(path: string) => request<T>('DELETE', path),
};
