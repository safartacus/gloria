import { authStorage } from '../auth/authStorage.ts';

/** Error carrying the HTTP status and the API's ProblemDetails body when present. */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  /** Trace id echoed by the API's exception middleware — quote it to IT for a 500. */
  readonly correlationId?: string;

  constructor(status: number, title: string, detail: string, correlationId?: string) {
    super(detail || title);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail || title;
    this.correlationId = correlationId;
  }
}

async function parseError(res: Response): Promise<ApiError> {
  let title = res.statusText;
  let detail = '';
  let correlationId: string | undefined;
  try {
    const problem = (await res.json()) as { title?: string; detail?: string; errors?: unknown; correlationId?: string };
    title = problem.title ?? title;
    if (problem.detail) detail = problem.detail;
    else if (problem.errors) detail = JSON.stringify(problem.errors);
    correlationId = problem.correlationId;
  } catch {
    /* non-JSON error body */
  }
  return new ApiError(res.status, title, detail, correlationId);
}

let refreshInFlight: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  const current = authStorage.get();
  if (!current?.refreshToken) return false;

  refreshInFlight ??= (async () => {
    try {
      const res = await fetch('/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: current.refreshToken }),
      });
      if (!res.ok) return false;
      const data = (await res.json()) as { accessToken: string; refreshToken: string; user: typeof current.user };
      authStorage.set({ accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user });
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

async function request<T>(method: string, path: string, body?: unknown, allowRetry = true): Promise<T> {
  const auth = authStorage.get();
  const isForm = body instanceof FormData;
  const res = await fetch(`/api${path}`, {
    method,
    headers: {
      ...(auth ? { Authorization: `Bearer ${auth.accessToken}` } : {}),
      // let the browser set the multipart boundary for FormData
      ...(body !== undefined && !isForm ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
  });

  if (res.status === 401 && allowRetry && !path.startsWith('/auth/') && auth?.refreshToken) {
    if (await tryRefresh()) return request<T>(method, path, body, false);
    authStorage.clear();
    throw new ApiError(401, 'Oturum sona erdi', 'Lütfen yeniden giriş yapın.');
  }

  if (!res.ok) throw await parseError(res);

  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  // no body arg => a bodyless POST (endpoint uses its defaults / query params)
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  postForm: <T>(path: string, form: FormData) => request<T>('POST', path, form),
};
