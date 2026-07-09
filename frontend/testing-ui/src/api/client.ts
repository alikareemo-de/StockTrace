import { apiUrl } from '../config';

export type ApiResult<T> =
  | { ok: true; status: number; data: T }
  | { ok: false; status: number; error: string; details?: unknown };

const jsonHeaders = {
  Accept: 'application/json',
  'Content-Type': 'application/json'
};

let accessToken: string | null = localStorage.getItem('stocktrace.accessToken');

export function setAccessToken(token: string | null) {
  accessToken = token;
  if (token) localStorage.setItem('stocktrace.accessToken', token);
  else localStorage.removeItem('stocktrace.accessToken');
}

export async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  return send<T>(url, { method: 'GET', headers: authHeaders({ Accept: 'application/json' }) });
}

export async function apiPost<T>(url: string, body: unknown): Promise<ApiResult<T>> {
  return send<T>(url, { method: 'POST', headers: authHeaders(jsonHeaders), body: JSON.stringify(body) });
}

export async function apiPut<T>(url: string, body: unknown): Promise<ApiResult<T>> {
  return send<T>(url, { method: 'PUT', headers: authHeaders(jsonHeaders), body: JSON.stringify(body) });
}

export async function apiDownload(url: string): Promise<ApiResult<Blob>> {
  try {
    const response = await fetch(apiUrl(url), { method: 'GET', headers: authHeaders({ Accept: '*/*' }) });
    if (!response.ok) {
      if (response.status === 401) window.dispatchEvent(new Event('stocktrace:unauthorized'));
      const text = await response.text();
      const payload = text ? JSON.parse(text) : null;
      return {
        ok: false,
        status: response.status,
        error: formatProblem(payload) || response.statusText,
        details: payload
      };
    }

    return { ok: true, status: response.status, data: await response.blob() };
  } catch (error) {
    return {
      ok: false,
      status: 0,
      error: error instanceof Error ? error.message : 'Download failed.'
    };
  }
}

async function send<T>(url: string, init: RequestInit): Promise<ApiResult<T>> {
  try {
    const response = await fetch(apiUrl(url), init);
    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (!response.ok) {
      if (response.status === 401) window.dispatchEvent(new Event('stocktrace:unauthorized'));
      return {
        ok: false,
        status: response.status,
        error: formatProblem(payload) || response.statusText,
        details: payload
      };
    }

    return { ok: true, status: response.status, data: payload as T };
  } catch (error) {
    return {
      ok: false,
      status: 0,
      error: error instanceof Error ? error.message : 'Request failed.'
    };
  }
}

function authHeaders(headers: Record<string, string>): Record<string, string> {
  return accessToken ? { ...headers, Authorization: `Bearer ${accessToken}` } : headers;
}

function formatProblem(payload: unknown): string | null {
  if (!payload || typeof payload !== 'object') return null;
  const problem = payload as { title?: string; errors?: Record<string, string[]> };
  if (problem.errors) {
    return Object.entries(problem.errors)
      .map(([key, messages]) => `${key}: ${messages.join(', ')}`)
      .join('\n');
  }

  return problem.title ?? null;
}
