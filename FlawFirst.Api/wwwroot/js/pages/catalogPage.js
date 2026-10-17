import { api } from "../api.js";
import { state, ensureCategories } from "../state.js";
import { esc, option, hashWith, route } from "../utils.js";
import { productCard } from "../components/productCard.js";
import { pager } from "../components/pager.js";

export async function catalogPage() {
  await ensureCategories();
  const q = route().query;
  const keyword = q.get("keyword") || "";
  const categoryId = q.get("categoryId") || "";
  const sort = q.get("sort") || "newest";
  const minPrice = q.get("minPrice") || "";
  const maxPrice = q.get("maxPrice") || "";
  const pageNo = q.get("page") || "1";

  const data = await api(`/api/products?${new URLSearchParams({
    keyword, categoryId, sort, minPrice, maxPrice, page: pageNo, pageSize: "9"
  })}`);

  const search = document.querySelector(".search input");
  if (search) search.value = keyword;

  const chips = [`<button class="chip ${categoryId ? "" : "on"}" data-action="category" data-id="" type="button">Tất cả</button>`]
    .concat(state.categories.map(category =>
      `<button class="chip ${String(category.id) === categoryId ? "on" : ""}" data-action="category" data-id="${category.id}" type="button">${esc(category.name)}</button>`
    )).join("");

  const cards = data.items.length
    ? data.items.map(productCard).join("")
    : `<div class="panel empty">Không có sản phẩm khớp bộ lọc.</div>`;

  return `
    <section class="hero">
      <div class="hero-copy">
        <p class="kicker">Flaw-first review</p>
        <h1>Đọc hạn chế thực tế trước khi tin số sao.</h1>
        <p>Mỗi bài viết tách riêng điểm hài lòng và hạn chế. Phần 1–2 sao được gom lại để thấy sản phẩm yếu ở đâu.</p>
        <div class="row" style="margin-top:16px"><a class="btn" href="#/write">Viết đánh giá</a></div>
      </div>
      <aside class="hero-panel">
        <strong>Một bài review gồm hai phần độc lập</strong>
        <div class="pair">
          <div class="pill-pros">Pros — điểm hài lòng</div>
          <div class="pill-cons">Cons — hạn chế thực tế</div>
        </div>
        <span class="muted">Có thể đăng ẩn danh. Tên và email sẽ không hiện ra ngoài.</span>
      </aside>
    </section>
    <div class="chips" style="margin-bottom:12px">${chips}</div>
    <form class="filters" data-form="filters">
      <label class="field"><span>Giá từ</span><input name="minPrice" type="number" min="0" value="${esc(minPrice)}" /></label>
      <label class="field"><span>Đến</span><input name="maxPrice" type="number" min="0" value="${esc(maxPrice)}" /></label>
      <label class="field"><span>Sắp xếp</span>
        <select name="sort">
          ${option("newest", "Mới nhất", sort)}
          ${option("rating", "Số sao cao", sort)}
          ${option("mostReviewed", "Nhiều review", sort)}
        </select>
      </label>
      <button class="btn" type="submit">Lọc</button>
    </form>
    <div class="grid">${cards}</div>
    ${pager(data.page, data.totalPages, (p) => hashWith({ page: p }))}
  `;
}
