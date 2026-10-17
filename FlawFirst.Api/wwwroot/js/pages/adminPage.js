import { api } from "../api.js";
import { state, ensureCategories } from "../state.js";
import { esc, when, route } from "../utils.js";

export async function adminPage() {
  if (state.user?.role !== "Admin") {
    return `<div class="auth-card"><h1>Chỉ dành cho quản trị</h1><p class="muted">Đăng nhập bằng admin@flawfirst.local hoặc header demo Admin.</p><a class="btn" href="#/login">Đăng nhập</a></div>`;
  }

  await ensureCategories();
  const tab = route().query.get("tab") || "reports";
  const tabs = `<div class="tabs">
    ${["reports", "users", "categories"].map(name => `<a class="chip ${tab === name ? "on" : ""}" href="#/admin?tab=${name}">${name === "reports" ? "Kiểm duyệt" : name === "users" ? "Người dùng" : "Ngành hàng"}</a>`).join("")}
  </div>`;

  if (tab === "users") return tabs + await usersAdmin();
  if (tab === "categories") return tabs + await categoriesAdmin();
  return tabs + await reportsAdmin();
}

async function reportsAdmin() {
  const status = route().query.get("status") || "Pending";
  const data = await api(`/api/admin/reports?status=${encodeURIComponent(status)}&pageSize=20`);
  const filters = ["Pending", "Resolved", "Dismissed", "All"].map(name =>
    `<a class="chip ${status === name ? "on" : ""}" href="#/admin?tab=reports&status=${name}">${name === "Pending" ? "Đang chờ" : name === "Resolved" ? "Đã xử lý" : name === "Dismissed" ? "Bỏ qua" : "Tất cả"}</a>`
  ).join("");

  const cards = data.items.map(report => `<article class="admin-card">
    <div class="row" style="justify-content:space-between"><strong>${esc(report.productName || report.reviewSnapshot || "Bài đã xóa")}</strong><span class="badge">${esc(report.status)}</span></div>
    <p class="muted">${esc(report.reporterName)} · ${when(report.createdAt)}</p>
    <p>${esc(report.reason)}</p>
    ${report.cons ? `<div class="box cons"><h4>Cons bị báo cáo</h4><p>${esc(report.cons)}</p></div>` : ""}
    ${report.status === "Pending" ? `<div class="actions">
      <button type="button" data-action="moderate" data-id="${report.id}" data-choice="Hide">Ẩn bài</button>
      <button type="button" data-action="moderate" data-id="${report.id}" data-choice="Delete">Xóa</button>
      <button type="button" data-action="moderate" data-id="${report.id}" data-choice="Penalize">Phạt</button>
      <button type="button" data-action="moderate" data-id="${report.id}" data-choice="Ignore">Bỏ qua</button>
    </div>` : `<p class="muted">Đã xử lý: ${esc(report.action || "")}</p>`}
  </article>`).join("") || `<div class="empty">Không có báo cáo.</div>`;

  return `<div class="chips" style="margin-bottom:14px">${filters}</div><div class="stack">${cards}</div>`;
}

async function usersAdmin() {
  const keyword = route().query.get("keyword") || "";
  const data = await api(`/api/admin/users?keyword=${encodeURIComponent(keyword)}&pageSize=20`);
  const rows = data.items.map(user => `<article class="admin-card row" style="justify-content:space-between">
    <div><strong>${esc(user.fullName)}</strong><div class="muted">${esc(user.email)} · ${esc(user.role)}</div></div>
    <div class="row">
      ${user.isLocked ? `<span class="badge warn">Đang khóa</span>` : `<span class="badge">Hoạt động</span>`}
      <button class="${user.isLocked ? "btn-soft" : "btn-danger"}" type="button" data-action="lock" data-id="${user.id}" data-locked="${user.isLocked ? "true" : "false"}">${user.isLocked ? "Mở khóa" : "Khóa"}</button>
    </div>
  </article>`).join("");

  return `<form class="filters" data-form="user-search"><label class="field grow"><span>Tìm tên hoặc email</span><input name="keyword" value="${esc(keyword)}" /></label><button class="btn" type="submit">Tìm</button></form><div class="stack">${rows}</div>`;
}

async function categoriesAdmin() {
  const cards = state.categories.map(category => `<article class="admin-card row" style="justify-content:space-between">
    <div><strong>${esc(category.name)}</strong><div class="muted">${esc(category.description || "")} · ${category.productCount} sản phẩm</div></div>
    <button class="btn-danger" type="button" data-action="delete-category" data-id="${category.id}">Xóa</button>
  </article>`).join("");

  return `<form class="panel pad form" data-form="category" style="margin-bottom:14px">
    <h3 style="margin:0">Thêm ngành hàng</h3>
    <label class="field"><span>Tên</span><input name="name" required minlength="2" /></label>
    <label class="field"><span>Mô tả</span><input name="description" /></label>
    <div id="form-error"></div>
    <button class="btn" type="submit">Lưu</button>
  </form><div class="stack">${cards}</div>`;
}
