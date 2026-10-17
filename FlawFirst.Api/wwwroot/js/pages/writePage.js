import { api } from "../api.js";
import { state, ensureCategories } from "../state.js";
import { esc, vnd, starSvg, route } from "../utils.js";
import { imageChip } from "../components/reviewCard.js";
import { needLogin } from "../ui.js";

export async function writePage() {
  if (!state.user) return needLogin();
  const q = route().query;
  let reviewId = Number(q.get("reviewId") || 0);
  let review = null;
  if (reviewId) review = await api(`/api/reviews/${reviewId}`);
  const productId = review?.productId || Number(q.get("productId") || 0);
  if (!productId) return productPicker(q.get("keyword") || "");

  if (!review) {
    const mine = await api("/api/reviews/mine?pageSize=50");
    review = (mine.items || []).find(item => item.productId === productId) || null;
    if (review) reviewId = review.id;
  }
  state.images = [...(review?.imageUrls || [])];
  const product = await api(`/api/products/${productId}`);
  const editing = Boolean(review);

  return `
    <section class="auth-card" style="max-width:720px">
      <p class="kicker">${editing ? "Sửa bài của bạn" : "Viết đánh giá"}</p>
      <h1>${esc(product?.name || "Sản phẩm")}</h1>
      <p class="muted">${editing ? "Bạn đã có một bài cho sản phẩm này. Sửa điểm hài lòng và hạn chế rồi lưu lại." : "Viết riêng điểm hài lòng và hạn chế thực tế. Có thể đăng ẩn danh."}</p>
      <form class="form" data-form="review">
        <input type="hidden" name="productId" value="${productId}" />
        <input type="hidden" name="reviewId" value="${reviewId || ""}" />
        <input type="hidden" name="rating" value="${review?.rating || 0}" />
        <div>
          <span class="label">Số sao</span>
          <div id="star-picker">${[1, 2, 3, 4, 5].map(n => `<button class="star-btn" type="button" data-action="rate" data-value="${n}" aria-label="${n} sao">${starSvg((review?.rating || 0) >= n)}</button>`).join("")}</div>
        </div>
        <label class="field"><span>Điểm hài lòng (Pros)</span><textarea name="pros" required minlength="10" maxlength="4000" placeholder="Điều bạn thấy ổn khi dùng">${esc(review?.pros || "")}</textarea></label>
        <label class="field"><span>Hạn chế thực tế (Cons)</span><textarea name="cons" required minlength="10" maxlength="4000" placeholder="Điều chưa ổn, cụ thể">${esc(review?.cons || "")}</textarea></label>
        <label class="row"><input type="checkbox" name="isAnonymous" ${review?.isAnonymous ? "checked" : ""} /> Đăng ẩn danh</label>
        <label class="field"><span>Ảnh minh chứng</span><input name="files" type="file" accept="image/*" multiple /></label>
        <div class="thumbs" id="image-list">${state.images.map(imageChip).join("")}</div>
        <div id="form-error"></div>
        <button class="btn" type="submit">${editing ? "Lưu thay đổi" : "Đăng bài"}</button>
      </form>
    </section>`;
}

export async function productPicker(keyword) {
  await ensureCategories();
  const creating = route().query.get("new") === "1";
  const data = await api(`/api/products?${new URLSearchParams({ keyword, pageSize: "12", sort: "newest" })}`);
  const items = data.items.length
    ? data.items.map(product => `<a class="pick" href="#/write?productId=${product.id}">
        <strong>${esc(product.name)}</strong>
        <span class="muted">${esc(product.brand)} · ${vnd(product.price)}</span>
      </a>`).join("")
    : `<div class="empty">Chưa có sản phẩm này trong danh mục.</div>`;

  const categories = state.categories.map(category => `<option value="${category.id}">${esc(category.name)}</option>`).join("");
  const createForm = creating ? `
    <form class="form" data-form="new-product" style="margin-bottom:18px">
      <h2 style="margin:0">Sản phẩm mới</h2>
      <p class="muted">Nhập sản phẩm chưa có trong danh mục. Sau khi tạo, bạn viết đánh giá ngay.</p>
      <label class="field"><span>Tên sản phẩm</span><input name="name" required minlength="2" maxlength="200" value="${esc(keyword)}" /></label>
      <label class="field"><span>Thương hiệu</span><input name="brand" required maxlength="120" /></label>
      <label class="field"><span>Ngành hàng</span><select name="categoryId" required>${categories}</select></label>
      <label class="field"><span>Giá (₫)</span><input name="price" type="number" min="0" step="1000" value="0" required /></label>
      <label class="field"><span>Mô tả</span><textarea name="description" maxlength="4000" placeholder="Không bắt buộc"></textarea></label>
      <div id="form-error"></div>
      <div class="row">
        <button class="btn" type="submit">Tạo và viết đánh giá</button>
        <a class="btn-ghost" href="#/write${keyword ? `?keyword=${encodeURIComponent(keyword)}` : ""}">Hủy</a>
      </div>
    </form>` : `<a class="btn" href="#/write?new=1${keyword ? `&keyword=${encodeURIComponent(keyword)}` : ""}">Thêm sản phẩm mới</a>`;

  return `
    <section class="auth-card" style="max-width:720px">
      <p class="kicker">Viết đánh giá</p>
      <h1>Chọn sản phẩm</h1>
      <p class="muted">Chọn sản phẩm đã có, hoặc thêm một sản phẩm hoàn toàn mới rồi viết Pros và Cons.</p>
      ${createForm}
      <form class="search" data-form="pick-product" style="max-width:none;margin:16px 0">
        <input name="keyword" value="${esc(keyword)}" placeholder="Tìm sản phẩm để đánh giá" aria-label="Tìm sản phẩm" />
        <button type="submit">Tìm</button>
      </form>
      <div class="stack">${items}</div>
    </section>`;
}
