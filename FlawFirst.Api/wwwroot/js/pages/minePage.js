import { api } from "../api.js";
import { state } from "../state.js";
import { route } from "../utils.js";
import { reviewCard } from "../components/reviewCard.js";
import { pager } from "../components/pager.js";
import { needLogin } from "../ui.js";

export async function minePage() {
  if (!state.user) return needLogin();
  const pageNo = route().query.get("page") || "1";
  const data = await api(`/api/reviews/mine?page=${pageNo}&pageSize=8`);
  const items = data.items.map(review => reviewCard(review, true)).join("") || `<div class="empty">Bạn chưa có bài review.</div>`;
  return `<div class="section-head"><h2>Bài của tôi</h2></div><div class="stack">${items}</div>${pager(data.page, data.totalPages, (p) => `#/mine?page=${p}`)}`;
}
