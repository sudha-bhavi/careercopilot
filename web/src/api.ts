import type { ApplicationInput, Dashboard, JobApplication, MatchResult, Status } from './types';

const TOKEN_KEY = 'jobpilot.token';

// The token lives in localStorage so a refresh keeps you signed in. Trade-off: any XSS bug
// could read it. That is why the API rejects non-http(s) links and React escapes all text.
export const getToken = () => localStorage.getItem(TOKEN_KEY);
export const clearToken = () => localStorage.removeItem(TOKEN_KEY);

let onUnauthorized: () => void = () => {};
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

async function readError(res: Response): Promise<string> {
  try {
    const body = await res.json();
    if (body?.errors) return Object.values(body.errors as Record<string, string[]>).flat().join(' ');
    if (body?.title) return body.title as string;
  } catch {
    // body was not JSON; fall through to the generic message
  }
  return `Request failed (${res.status}).`;
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();
  const res = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (res.status === 401) {
    // Token missing, expired or invalid: drop it and send the user back to the login screen.
    clearToken();
    onUnauthorized();
    throw new Error('Your session has ended. Please sign in again.');
  }
  if (!res.ok) throw new Error(await readError(res));
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export async function login(username: string, password: string): Promise<void> {
  const res = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });
  if (res.status === 401) throw new Error('Invalid username or password.');
  if (res.status === 429) throw new Error('Too many attempts. Wait a minute and try again.');
  if (!res.ok) throw new Error(`Sign-in failed (${res.status}).`);
  const body = (await res.json()) as { token: string };
  localStorage.setItem(TOKEN_KEY, body.token);
}

export const listApplications = (status?: Status) =>
  request<JobApplication[]>(`/api/applications${status ? `?status=${status}` : ''}`);

export const createApplication = (input: ApplicationInput) =>
  request<JobApplication>('/api/applications', { method: 'POST', body: JSON.stringify(input) });

export const updateApplication = (id: string, input: ApplicationInput) =>
  request<JobApplication>(`/api/applications/${id}`, { method: 'PUT', body: JSON.stringify(input) });

export const deleteApplication = (id: string) =>
  request<void>(`/api/applications/${id}`, { method: 'DELETE' });

export const getDashboard = () => request<Dashboard>('/api/dashboard');

export const matchResume = (resumeText: string, jobDescription: string) =>
  request<MatchResult>('/api/resume/match', {
    method: 'POST',
    body: JSON.stringify({ resumeText, jobDescription }),
  });
