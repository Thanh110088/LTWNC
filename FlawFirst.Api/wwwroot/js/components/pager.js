export function pager(page, totalPages, makeHref) {
  if (totalPages <= 1) return "";
  const prev = page > 1 ? `<a class="btn-ghost" href="${makeHref(page - 1)}">Trước</a>` : "";
  const next = page < totalPages ? `<a class="btn-ghost" href="${makeHref(page + 1)}">Sau</a>` : "";
  return `<div class="pager">${prev}<span class="muted">Trang ${page}/${totalPages}</span>${next}</div>`;
}
