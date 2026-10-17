import { api } from "../api.js";
import { esc, when } from "../utils.js";
import { state } from "../state.js";

export async function toggleComments(reviewId, forceOpen = false) {
  const box = document.getElementById(`comments-${reviewId}`);
  if (!box) return;

  if (box.dataset.open === "1" && !forceOpen) {
    box.hidden = true;
    box.dataset.open = "0";
    return;
  }

  box.hidden = false;
  const data = await api(`/api/social/reviews/${reviewId}/comments?pageSize=20`);
  const items = data.items.map(comment =>
    `<div class="comment"><strong>${esc(comment.authorName)}</strong> <span class="muted">${when(comment.createdAt)}</span><div>${esc(comment.content)}</div></div>`
  ).join("");

  const form = state.user ? `<form class="inline-form" data-form="comment" style="margin-top:8px">
      <input type="hidden" name="reviewId" value="${reviewId}" />
      <input name="content" placeholder="Viết bình luận" required />
      <label class="row"><input type="checkbox" name="isAnonymous" /> Ẩn danh</label>
      <button class="btn" type="submit">Gửi</button>
    </form>` : "";

  box.innerHTML = `${items || `<p class="muted">Chưa có bình luận.</p>`}${form}<div id="form-error"></div>`;
  box.dataset.open = "1";
}
