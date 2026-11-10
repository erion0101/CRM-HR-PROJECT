import axios, { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || '/api/v1';
const safeMethods = new Set(['GET', 'HEAD', 'OPTIONS']);
export const unauthorizedEvent = 'hr:unauthorized';
let antiforgeryToken: string | null = null;
let antiforgeryRequest: Promise<string> | null = null;

export const http = axios.create({
  baseURL: apiBaseUrl,
  timeout: 20_000,
  withCredentials: true,
  headers: { Accept: 'application/json' }
});

http.interceptors.request.use(async (config) => {
  const method = (config.method ?? 'GET').toUpperCase();
  if (!safeMethods.has(method) && !config.url?.endsWith('/auth/login')) {
    const token = await getAntiforgeryToken();
    const headers = config.headers instanceof AxiosHeaders ? config.headers : new AxiosHeaders(config.headers);
    headers.set('X-CSRF-TOKEN', token);
    config.headers = headers;
  }
  return config;
});

http.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (
      axios.isAxiosError(error) &&
      error.response?.status === 400 &&
      isAntiforgeryError(error.response.data) &&
      error.config
    ) {
      const request = error.config as InternalAxiosRequestConfig & { _csrfRetry?: boolean };
      if (!request._csrfRetry) {
        request._csrfRetry = true;
        antiforgeryToken = null;
        request.headers.set('X-CSRF-TOKEN', await getAntiforgeryToken());
        return http.request(request);
      }
    }
    if (axios.isAxiosError(error) && error.response?.status === 401 && !error.config?.url?.endsWith('/auth/login')) {
      window.dispatchEvent(new Event(unauthorizedEvent));
    }
    throw error;
  }
);

export function clearAntiforgeryToken(): void {
  antiforgeryToken = null;
}

export async function getAntiforgeryToken(): Promise<string> {
  if (antiforgeryToken) return antiforgeryToken;
  if (!antiforgeryRequest) {
    antiforgeryRequest = axios
      .get<{ token: string }>(`${apiBaseUrl}/auth/antiforgery`, { withCredentials: true })
      .then((response) => {
        antiforgeryToken = response.data.token;
        return response.data.token;
      })
      .finally(() => {
        antiforgeryRequest = null;
      });
  }
  return antiforgeryRequest;
}

function isAntiforgeryError(data: unknown): boolean {
  return Boolean(
    data && typeof data === 'object' && 'title' in data && String(data.title).toLowerCase().includes('antiforgery')
  );
}
