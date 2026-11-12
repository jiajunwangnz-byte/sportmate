export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  const userId = localStorage.getItem("sportmate-user");
  if (userId) headers.set("X-User-Id", userId);
  const base = import.meta.env.VITE_API_URL ?? "";
  const response = await fetch(`${base}${path}`, { ...options, headers });
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  const data = text ? JSON.parse(text) as { message?: string } : null;
  if (!response.ok) throw new Error(data?.message ?? `请求失败 (${response.status})`);
  return data as T;
}
