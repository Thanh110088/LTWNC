import { esc } from "./utils.js";

export function toast(message) {
  const el = document.getElementById("toast");
  if (!el) return;
  el.hidden = false;
  el.textContent = message;
  clearTimeout(toast.timer);
  toast.timer = setTimeout(() => {
    el.hidden = true;
  }, 2800);
}

export function openModal(html) {
  const modal = document.getElementById("modal");
  if (!modal) return;
  modal.hidden = false;
  modal.innerHTML = `<div class="modal-back"><div class="modal" role="dialog">${html}</div></div>`;
}

export function closeModal() {
  const modal = document.getElementById("modal");
  if (!modal) return;
  modal.hidden = true;
  modal.innerHTML = "";
}

export function showFormError(message) {
  const box = document.getElementById("form-error");
  if (box) {
    box.innerHTML = `<div class="alert">${esc(message)}</div>`;
  } else {
    toast(message);
  }
}

export function needLogin() {
  return `<div class="auth-card"><h1>Cần đăng nhập</h1><a class="btn" href="#/login">Đăng nhập</a></div>`;
}
