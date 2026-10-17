import { loadMe } from "./api.js";
import { state, refreshSocial } from "./state.js";
import { esc, route, go } from "./utils.js";
import { closeModal } from "./ui.js";
import { renderNav } from "./components/nav.js";
import { initHandlers } from "./handlers.js";

// Pages
import { catalogPage } from "./pages/catalogPage.js";
import { productPage } from "./pages/productPage.js";
import { writePage } from "./pages/writePage.js";
import { minePage } from "./pages/minePage.js";
import { savedPage } from "./pages/savedPage.js";
import { authPage } from "./pages/authPage.js";
import { adminPage } from "./pages/adminPage.js";
import { userPage } from "./pages/userPage.js";

async function page() {
  const { parts } = route();
  if (parts.length === 0) return catalogPage();
  if (parts[0] === "product" && parts[1]) return productPage(Number(parts[1]));
  if (parts[0] === "write") return writePage();
  if (parts[0] === "mine") return minePage();
  if (parts[0] === "saved") return savedPage();
  if (parts[0] === "login") return authPage(false);
  if (parts[0] === "register") return authPage(true);
  if (parts[0] === "admin") return adminPage();
  if (parts[0] === "user" && parts[1]) return userPage(Number(parts[1]));
  return `<div class="empty">Không tìm thấy trang.</div>`;
}

let renderToken = 0;
export async function render() {
  const token = ++renderToken;
  renderNav();
  const main = document.getElementById("main");
  if (!main) return;

  main.innerHTML = `<div class="loading">Đang tải...</div>`;
  try {
    const html = await page();
    if (token !== renderToken) return;
    main.innerHTML = html;
  } catch (error) {
    if (token !== renderToken) return;
    main.innerHTML = `<div class="panel empty">${esc(error.message)}</div>`;
  }
}

async function boot() {
  state.user = await loadMe();
  await refreshSocial();
  renderNav();
  await render();
}

initHandlers({ render, go });

window.addEventListener("hashchange", () => {
  closeModal();
  render();
});

boot();
