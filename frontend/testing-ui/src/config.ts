const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/$/, '') ?? '';

export const appConfig = {
  apiBaseUrl: configuredApiBaseUrl
};

export function apiUrl(path: string): string {
  return appConfig.apiBaseUrl ? `${appConfig.apiBaseUrl}${path}` : path;
}
