import { esc, stars, when } from "../utils.js";
import { state } from "../state.js";

export function imageChip(url) {
  return `<span class="thumb-mini" style="display:inline-grid;place-items:end"><img src="${esc(url)}" alt="" /><button type="button" data-action="remove-image" data-url="${esc(url)}">×</button></span>`;
}

export function reviewCard(review, mine = false) {
  const images = (review.imageUrls || []).map(url => `<img src="${esc(url)}" alt="Ảnh minh chứng" />`).join("");
  const saved = state.bookmarks.has(review.id);
  const followed = review.authorId && state.following.has(review.authorId);
  const follow = review.authorId && state.user && review.authorId !== state.user.id
    ? `<button type="button" data-action="follow" data-id="${review.authorId}">${followed ? "Bỏ theo dõi" : "Theo dõi"}</button>`
    : "";
  const author = review.authorId
    ? `<a class="who" href="#/user/${review.authorId}">${esc(review.authorName)}</a>`
    : `<span class="who">${esc(review.authorName)}</span>`;
  const ownerActions = mine || (state.user && review.authorId === state.user.id)
    ? `<a class="btn-soft" href="#/write?reviewId=${review.id}">Sửa</a>
       <button class="btn-danger" type="button" data-action="delete-review" data-id="${review.id}">Xóa</button>`
    : "";

  return `<article class="review" id="review-${review.id}">
    <div class="review-top">
      <div>${stars(review.rating)} ${author} ${review.isAnonymous ? `<span class="badge">Ẩn danh</span>` : ""} ${review.isHidden ? `<span class="badge warn">Đã bị ẩn</span>` : ""}</div>
      <span class="muted">${when(review.createdAt)}</span>
    </div>
    <p class="muted" style="margin:6px 0 0">${esc(review.productName || "")}</p>
    <div class="split">
      <div class="box pros"><h4>Điểm hài lòng</h4><p>${esc(review.pros)}</p></div>
      <div class="box cons"><h4>Hạn chế thực tế</h4><p>${esc(review.cons)}</p></div>
    </div>
    ${images ? `<div class="thumbs">${images}</div>` : ""}
    <div class="actions">
      <button type="button" data-action="vote" data-id="${review.id}" data-vote="Up">Hữu ích <span data-up="${review.id}">${review.upvoteCount}</span></button>
      <button type="button" data-action="vote" data-id="${review.id}" data-vote="Down">Không <span data-down="${review.id}">${review.downvoteCount}</span></button>
      <button type="button" data-action="comments" data-id="${review.id}">Bình luận ${review.commentCount}</button>
      <button type="button" class="${saved ? "on" : ""}" data-action="bookmark" data-id="${review.id}">${saved ? "Đã lưu" : "Lưu"}</button>
      <button type="button" data-action="share" data-id="${review.id}">Chia sẻ</button>
      <button type="button" data-action="report" data-id="${review.id}">Báo cáo</button>
      ${follow}
      ${ownerActions}
    </div>
    <div class="comments" id="comments-${review.id}" hidden></div>
  </article>`;
}
