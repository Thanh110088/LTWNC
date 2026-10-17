import { esc, route } from "../utils.js";
import { getToken, isExplicitLogout } from "../api.js";
import { state } from "../state.js";

export function renderNav() {
  const current = route().path;
  const user = state.user;
  const links = [
    ["/", "Khám phá"],
    ["/write", "Viết đánh giá"],
    ["/mine", "Bài của tôi"],
    ["/saved", "Đã lưu"]
  ];

  if (user?.role === "Admin") {
    links.push(["/admin", "Quản trị"]);
  }

  const linkHtml = links.map(([href, label]) => {
    const active = current === href || (href !== "/" && current.startsWith(href)) ? "active" : "";
    return `<a class="${active}" href="#${href}">${label}</a>`;
  }).join("");

  let account = `<a class="btn" href="#/login">Đăng nhập</a>`;
  if (user) {
    const demo = !getToken() && !isExplicitLogout() ? `<span class="badge">Demo</span>` : "";
    account = `<span class="userchip"><span class="avatar">${esc(user.fullName.charAt(0))}</span>${esc(user.fullName)} ${demo}</span>
      <button class="btn-ghost" data-action="logout" type="button">Thoát</button>`;
  }

  const navEl = document.getElementById("nav");
  if (navEl) {
    navEl.innerHTML = `${linkHtml}${account}`;
  }
}
