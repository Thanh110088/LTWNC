import { api } from "../api.js";
import { state } from "../state.js";
import { esc, route } from "../utils.js";
import { reviewCard } from "../components/reviewCard.js";
import { pager } from "../components/pager.js";
import { needLogin } from "../ui.js";

export async function savedPage() {
  if (!state.user) return needLogin();
  const tab = route().query.get("tab") || "bookmarks";
  const pageNo = route().query.get("page") || "1";
  const tabs = `<div class="tabs">
    <a class="chip ${tab === "bookmarks" ? "on" : ""}" href="#/saved?tab=bookmarks">Đã lưu</a>
    <a class="chip ${tab === "following" ? "on" : ""}" href="#/saved?tab=following">Đang theo dõi</a>
  </div>`;

  if (tab === "following") {
    const people = await api("/api/social/following");
    const list = people.length
      ? people.map(person => `<article class="admin-card row" style="justify-content:space-between"><a href="#/user/${person.userId}"><strong>${esc(person.fullName)}</strong></a><button class="btn-ghost" data-action="follow" data-id="${person.userId}" type="button">Bỏ theo dõi</button></article>`).join("")
      : `<div class="empty">Bạn chưa theo dõi ai.</div>`;
    return `${tabs}<div class="stack">${list}</div>`;
  }

  const data = await api(`/api/social/bookmarks?page=${pageNo}&pageSize=8`);
  const items = data.items.map(item => reviewCard(item.review)).join("") || `<div class="empty">Chưa lưu bài nào.</div>`;
  return `${tabs}<div class="stack">${items}</div>${pager(data.page, data.totalPages, (p) => `#/saved?tab=bookmarks&page=${p}`)}`;
}
