const hues = ["#0e7c74", "#b7791f", "#c2410c", "#3f6212", "#1d4ed8", "#7c3aed"];

export function esc(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

export function vnd(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
  }).format(value || 0);
}

export function when(value) {
  if (!value) return "";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium" }).format(new Date(value));
}

export function starSvg(on) {
  return `<svg class="star ${on ? "on" : ""}" viewBox="0 0 20 20" aria-hidden="true"><path d="M10 1.8 12.4 7l5.6.5-4.2 3.7 1.3 5.5L10 14.2 4.9 16.7 6.2 11.2 2 7.5 7.6 7z"/></svg>`;
}

export function stars(value) {
  const rounded = Math.round(Number(value) || 0);
  let html = `<span class="stars" aria-label="${esc(value)} trên 5 sao">`;
  for (let i = 1; i <= 5; i += 1) html += starSvg(i <= rounded);
  html += "</span>";
  return html;
}

export function hue(text) {
  const source = String(text || "F");
  return hues[source.charCodeAt(0) % hues.length];
}

export function thumb(name, imageUrl) {
  if (imageUrl) return `<div class="thumb"><img src="${esc(imageUrl)}" alt="" /></div>`;
  const letter = esc((name || "?").trim().charAt(0).toUpperCase());
  return `<div class="thumb" style="background:${hue(name)}">${letter}</div>`;
}

export function option(value, label, current) {
  return `<option value="${value}" ${value === current ? "selected" : ""}>${label}</option>`;
}

export function route() {
  const raw = (location.hash || "#/").slice(1);
  const [path, query = ""] = raw.split("?");
  const parts = path.split("/").filter(Boolean);
  return { parts, query: new URLSearchParams(query), path: `/${parts.join("/")}` || "/" };
}

export function go(path) {
  location.hash = path.startsWith("#") ? path.slice(1) : path;
}

export function hashWith(changes) {
  const currentRoute = route();
  const q = currentRoute.query;
  const path = `/${currentRoute.parts.join("/")}`;
  for (const [key, value] of Object.entries(changes)) {
    if (value === "" || value == null) q.delete(key);
    else q.set(key, value);
  }
  const text = q.toString();
  return `#${path}${text ? `?${text}` : ""}`;
}
