const TOKEN_KEY = "flawfirst.token";
const LOGOUT_KEY = "flawfirst.loggedOut";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function setSession(token) {
  localStorage.setItem(TOKEN_KEY, token);
  sessionStorage.removeItem(LOGOUT_KEY);
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  sessionStorage.setItem(LOGOUT_KEY, "1");
}

export function isExplicitLogout() {
  return sessionStorage.getItem(LOGOUT_KEY) === "1";
}

export async function api(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = getToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  else if (isExplicitLogout()) headers.set("Authorization", "Bearer guest");

  let body = options.body;
  if (body && !(body instanceof FormData) && typeof body !== "string") {
    headers.set("Content-Type", "application/json");
    body = JSON.stringify(body);
  }

  const response = await fetch(path, { ...options, headers, body });
  if (response.status === 204) return null;

  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) {
    const errors = Array.isArray(data?.errors) ? ` ${data.errors.join(" ")}` : "";
    const error = new Error(`${data?.message || "Có lỗi xảy ra."}${errors}`.trim());
    error.status = response.status;
    throw error;
  }
  return data;
}

export async function loadMe() {
  try {
    return await api("/api/auth/me");
  } catch {
    return null;
  }
}
