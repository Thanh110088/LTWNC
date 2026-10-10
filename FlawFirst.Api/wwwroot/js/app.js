import { api, clearSession, getToken, isExplicitLogout, loadMe, setSession } from "./api.js";

const state = {
  user: null,
  categories: [],
  bookmarks: new Set(),
  following: new Set(),
  images: []
};

const hues = ["#0e7c74", "#b7791f", "#c2410c", "#3f6212", "#1d4ed8", "#7c3aed"];

function esc(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function vnd(value) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(value || 0);
}

function when(value) {
  if (!value) return "";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium" }).format(new Date(value));
}

function starSvg(on) {
  return `<svg class="star ${on ? "on" : ""}" viewBox="0 0 20 20" aria-hidden="true"><path d="M10 1.8 12.4 7l5.6.5-4.2 3.7 1.3 5.5L10 14.2 4.9 16.7 6.2 11.2 2 7.5 7.6 7z"/></svg>`;
}

function stars(value) {
  const rounded = Math.round(Number(value) || 0);
  let html = `<span class="stars" aria-label="${esc(value)} trên 5 sao">`;
  for (let i = 1; i <= 5; i += 1) html += starSvg(i <= rounded);
  html += "</span>";
  return html;
}

function hue(text) {
  const source = String(text || "F");
  return hues[source.charCodeAt(0) % hues.length];
}

function thumb(name, imageUrl) {
  if (imageUrl) return `<div class="thumb"><img src="${esc(imageUrl)}" alt="" /></div>`;
  const letter = esc((name || "?").trim().charAt(0).toUpperCase());
  return `<div class="thumb" style="background:${hue(name)}">${letter}</div>`;
}

function route() {
  const raw = (location.hash || "#/").slice(1);
  const [path, query = ""] = raw.split("?");
  const parts = path.split("/").filter(Boolean);
  return { parts, query: new URLSearchParams(query), path: `/${parts.join("/")}` || "/" };
}

function go(path) {
  location.hash = path.startsWith("#") ? path.slice(1) : path;
}

function toast(message) {
  const el = document.getElementById("toast");
  el.hidden = false;
  el.textContent = message;
  clearTimeout(toast.timer);
  toast.timer = setTimeout(() => { el.hidden = true; }, 2800);
}

function closeModal() {
  const modal = document.getElementById("modal");
  modal.hidden = true;
  modal.innerHTML = "";
}

function openModal(html) {
  const modal = document.getElementById("modal");
  modal.hidden = false;
  modal.innerHTML = `<div class="modal-back"><div class="modal" role="dialog">${html}</div></div>`;
}

function pager(page, totalPages, makeHref) {
  if (totalPages <= 1) return "";
  const prev = page > 1 ? `<a class="btn-ghost" href="${makeHref(page - 1)}">Trước</a>` : "";
  const next = page < totalPages ? `<a class="btn-ghost" href="${makeHref(page + 1)}">Sau</a>` : "";
  return `<div class="pager">${prev}<span class="muted">Trang ${page}/${totalPages}</span>${next}</div>`;
}

function renderNav() {
  const current = route().path;
  const user = state.user;
  const links = [
    ["/", "Khám phá"],
    ["/write", "Viết đánh giá"],
    ["/mine", "Bài của tôi"],
    ["/saved", "Đã lưu"]
  ];
  if (user?.role === "Admin") links.push(["/admin", "Quản trị"]);
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
  document.getElementById("nav").innerHTML = `${linkHtml}${account}`;
}

let renderToken = 0;
async function render() {
  const token = ++renderToken;
  renderNav();
  const main = document.getElementById("main");
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

async function ensureCategories() {
  if (state.categories.length === 0)
    state.categories = await api("/api/products/categories");
}

async function catalogPage() {
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

function option(value, label, current) {
  return `<option value="${value}" ${value === current ? "selected" : ""}>${label}</option>`;
}

function productCard(product) {
  return `<article class="card product">
    <a class="cover" href="#/product/${product.id}">
      ${thumb(product.name, product.imageUrl)}
      <div class="product-body">
        <p class="kicker">${esc(product.brand)} · ${esc(product.categoryName)}</p>
        <h3>${esc(product.name)}</h3>
        <p class="price">${vnd(product.price)}</p>
        <p class="meta">${stars(product.avgRating)} <span>${Number(product.avgRating).toFixed(1)} · ${product.totalReviews} review</span></p>
      </div>
    </a>
    <a class="btn write-link" href="#/write?productId=${product.id}">Viết đánh giá</a>
  </article>`;
}

function hashWith(changes) {
  const q = route().query;
  const { parts } = route();
  const path = `/${parts.join("/")}`;
  for (const [key, value] of Object.entries(changes)) {
    if (value === "" || value == null) q.delete(key);
    else q.set(key, value);
  }
  const text = q.toString();
  return `#${path}${text ? `?${text}` : ""}`;
}

async function productPage(id) {
  const q = route().query;
  const star = q.get("star") || "";
  const hasImages = q.get("hasImages") || "";
  const sort = q.get("sort") || "newest";
  const pageNo = q.get("page") || "1";
  const params = new URLSearchParams({ sort, page: pageNo, pageSize: "6" });
  if (star) params.set("star", star);
  if (hasImages) params.set("hasImages", hasImages);

  const [product, reviews, flaws] = await Promise.all([
    api(`/api/products/${id}`),
    api(`/api/products/${id}/reviews?${params}`),
    api(`/api/products/${id}/flaws`)
  ]);

  const dist = product.starDistribution;
  const bars = [5, 4, 3, 2, 1].map(level => {
    const count = dist[`star${level}`] || 0;
    const width = product.totalReviews ? Math.round(count * 100 / product.totalReviews) : 0;
    return `<div class="bar-row"><span>${level}★</span><div class="track"><span style="width:${width}%"></span></div><span>${count}</span></div>`;
  }).join("");

  const flawsHtml = flaws.flaws.length
    ? flaws.flaws.map(flaw => `<article class="flaw"><span class="badge hot">${flaw.rating} sao</span><p>${esc(flaw.cons)}</p></article>`).join("")
    : `<div class="empty">Chưa có review 1–2 sao.</div>`;

  return `
    <p class="kicker"><a href="#/">Khám phá</a> / ${esc(product.categoryName)}</p>
    <div class="layout">
      <aside class="stack">
        <div class="card">${thumb(product.name, product.imageUrl)}
          <div class="pad">
            <p class="kicker">${esc(product.brand)}</p>
            <h2 style="margin:6px 0">${esc(product.name)}</h2>
            <p class="price">${vnd(product.price)}</p>
            <p>${esc(product.description || "")}</p>
            <a class="btn" href="#/write?productId=${product.id}">Viết đánh giá</a>
          </div>
        </div>
        <div class="stat">
          <div class="big">${Number(product.avgRating).toFixed(1)}</div>
          <div>${stars(product.avgRating)}</div>
          <p class="muted">${product.totalReviews} đánh giá</p>
          <div class="bars">${bars}</div>
        </div>
      </aside>
      <section>
        <div class="section-head">
          <div>
            <p class="kicker">Flaw-first</p>
            <h2>Hạn chế thực tế</h2>
          </div>
          <span class="badge hot">${flaws.flawReviewCount} bài 1–2 sao</span>
        </div>
        <div class="flaw-list">${flawsHtml}</div>
        <div class="section-head" style="margin-top:28px">
          <h2>Tất cả đánh giá</h2>
          <a class="btn" href="#/write?productId=${product.id}">Viết đánh giá</a>
        </div>
        <div class="chips">
          ${[ "", "5", "4", "3", "2", "1" ].map(value => `<a class="chip ${star === value ? "on" : ""}" href="${hashWith({ star: value, page: 1 })}">${value ? `${value} sao` : "Mọi sao"}</a>`).join("")}
          <a class="chip ${hasImages === "true" ? "on" : ""}" href="${hashWith({ hasImages: hasImages === "true" ? "" : "true", page: 1 })}">Có ảnh</a>
          <a class="chip ${sort === "newest" ? "on" : ""}" href="${hashWith({ sort: "newest", page: 1 })}">Mới nhất</a>
          <a class="chip ${sort === "rating" ? "on" : ""}" href="${hashWith({ sort: "rating", page: 1 })}">Theo sao</a>
        </div>
        <div class="stack" style="margin-top:14px">${reviews.items.map(review => reviewCard(review)).join("") || `<div class="empty">Chưa có bài phù hợp.</div>`}</div>
        ${pager(reviews.page, reviews.totalPages, (p) => hashWith({ page: p }))}
      </section>
    </div>`;
}

function reviewCard(review, mine = false) {
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

async function writePage() {
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

async function productPicker(keyword) {
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

function imageChip(url) {
  return `<span class="thumb-mini" style="display:inline-grid;place-items:end"><img src="${esc(url)}" alt="" /><button type="button" data-action="remove-image" data-url="${esc(url)}">×</button></span>`;
}

async function minePage() {
  if (!state.user) return needLogin();
  const pageNo = route().query.get("page") || "1";
  const data = await api(`/api/reviews/mine?page=${pageNo}&pageSize=8`);
  const items = data.items.map(review => reviewCard(review, true)).join("") || `<div class="empty">Bạn chưa có bài review.</div>`;
  return `<div class="section-head"><h2>Bài của tôi</h2></div><div class="stack">${items}</div>${pager(data.page, data.totalPages, (p) => `#/mine?page=${p}`)}`;
}

async function savedPage() {
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

function needLogin() {
  return `<div class="auth-card"><h1>Cần đăng nhập</h1><a class="btn" href="#/login">Đăng nhập</a></div>`;
}

function authPage(isRegister) {
  return `<section class="auth-card">
    <p class="kicker">${isRegister ? "Tạo tài khoản" : "Chào trở lại"}</p>
    <h1>${isRegister ? "Đăng ký" : "Đăng nhập"}</h1>
    <form class="form" data-form="${isRegister ? "register" : "login"}">
      ${isRegister ? `<label class="field"><span>Họ tên</span><input name="fullName" required minlength="2" /></label>` : ""}
      <label class="field"><span>Email</span><input name="email" type="email" required /></label>
      <label class="field"><span>Mật khẩu</span><input name="password" type="password" required minlength="6" /></label>
      <div id="form-error"></div>
      <button class="btn" type="submit">${isRegister ? "Tạo tài khoản" : "Đăng nhập"}</button>
    </form>
    <p class="muted">${isRegister ? `Đã có tài khoản? <a href="#/login">Đăng nhập</a>` : `Chưa có tài khoản? <a href="#/register">Đăng ký</a>`}</p>
  </section>`;
}

async function adminPage() {
  if (state.user?.role !== "Admin") return `<div class="auth-card"><h1>Chỉ dành cho quản trị</h1><p class="muted">Đăng nhập bằng admin@flawfirst.local hoặc header demo Admin.</p><a class="btn" href="#/login">Đăng nhập</a></div>`;
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

async function userPage(id) {
  const profile = await api(`/api/social/users/${id}`);
  const followed = state.following.has(id);
  const canFollow = state.user && state.user.id !== id;
  return `<section class="auth-card">
    <span class="avatar" style="width:54px;height:54px;font-size:22px">${esc(profile.fullName.charAt(0))}</span>
    <h1>${esc(profile.fullName)}</h1>
    <p class="muted">${profile.followerCount} người theo dõi · đang theo dõi ${profile.followingCount}</p>
    ${canFollow ? `<button class="btn" type="button" data-action="follow" data-id="${id}">${followed || profile.isFollowing ? "Bỏ theo dõi" : "Theo dõi"}</button>` : ""}
  </section>`;
}

function showFormError(message) {
  const box = document.getElementById("form-error");
  if (box) box.innerHTML = `<div class="alert">${esc(message)}</div>`;
  else toast(message);
}

async function refreshSocial() {
  if (!state.user) {
    state.bookmarks = new Set();
    state.following = new Set();
    return;
  }
  try {
    const [bookmarks, following] = await Promise.all([
      api("/api/social/bookmarks?pageSize=50"),
      api("/api/social/following")
    ]);
    state.bookmarks = new Set(bookmarks.items.map(item => item.reviewId));
    state.following = new Set(following.map(item => item.userId));
  } catch {
    state.bookmarks = new Set();
    state.following = new Set();
  }
}

async function boot() {
  state.user = await loadMe();
  await refreshSocial();
  renderNav();
  await render();
}

document.body.addEventListener("click", async (event) => {
  if (event.target.closest(".modal-back") && event.target.classList.contains("modal-back")) closeModal();
  const el = event.target.closest("[data-action]");
  if (!el) return;
  const action = el.dataset.action;
  try {
    if (action === "logout") {
      clearSession();
      state.user = null;
      await refreshSocial();
      go("/");
      return;
    }
    if (action === "category") {
      go(hashWith({ categoryId: el.dataset.id, page: 1 }).slice(1));
      return;
    }
    if (action === "rate") {
      const value = Number(el.dataset.value);
      const input = document.querySelector('input[name="rating"]');
      if (input) input.value = String(value);
      document.querySelectorAll("[data-action='rate']").forEach(button => {
        const on = Number(button.dataset.value) <= value;
        button.innerHTML = starSvg(on);
      });
      return;
    }
    if (action === "remove-image") {
      state.images = state.images.filter(url => url !== el.dataset.url);
      const list = document.getElementById("image-list");
      if (list) list.innerHTML = state.images.map(imageChip).join("");
      return;
    }
    if (action === "vote") {
      const result = await api(`/api/social/reviews/${el.dataset.id}/votes`, { method: "POST", body: { voteType: el.dataset.vote } });
      const up = document.querySelector(`[data-up="${el.dataset.id}"]`);
      const down = document.querySelector(`[data-down="${el.dataset.id}"]`);
      if (up) up.textContent = result.upvoteCount;
      if (down) down.textContent = result.downvoteCount;
      toast(result.currentVote ? `Đã ghi nhận ${result.currentVote === "Up" ? "hữu ích" : "không hữu ích"}` : "Đã gỡ vote");
      return;
    }
    if (action === "bookmark") {
      const id = Number(el.dataset.id);
      if (state.bookmarks.has(id)) {
        await api(`/api/social/reviews/${id}/bookmarks`, { method: "DELETE" });
        state.bookmarks.delete(id);
        el.textContent = "Lưu";
        el.classList.remove("on");
      } else {
        await api(`/api/social/reviews/${id}/bookmarks`, { method: "POST" });
        state.bookmarks.add(id);
        el.textContent = "Đã lưu";
        el.classList.add("on");
      }
      return;
    }
    if (action === "share") {
      const link = await api(`/api/social/reviews/${el.dataset.id}/share`);
      await navigator.clipboard.writeText(link.shareUrl);
      toast("Đã chép link chia sẻ");
      return;
    }
    if (action === "comments") {
      await toggleComments(el.dataset.id);
      return;
    }
    if (action === "report") {
      openModal(`<h3>Báo cáo bài review</h3>
        <form class="form" data-form="report">
          <input type="hidden" name="reviewId" value="${el.dataset.id}" />
          <label class="field"><span>Lý do</span><textarea name="reason" required minlength="10"></textarea></label>
          <label class="field"><span>Ảnh bằng chứng</span><input name="files" type="file" accept="image/*" /></label>
          <div id="form-error"></div>
          <div class="row"><button class="btn" type="submit">Gửi</button><button class="btn-ghost" type="button" data-action="close">Đóng</button></div>
        </form>`);
      return;
    }
    if (action === "close") { closeModal(); return; }
    if (action === "follow") {
      const id = Number(el.dataset.id);
      if (state.following.has(id)) {
        await api(`/api/social/users/${id}/follow`, { method: "DELETE" });
        state.following.delete(id);
        toast("Đã bỏ theo dõi");
      } else {
        await api(`/api/social/users/${id}/follow`, { method: "POST" });
        state.following.add(id);
        toast("Đã theo dõi");
      }
      await render();
      return;
    }
    if (action === "delete-review") {
      if (!confirm("Xóa bài review này?")) return;
      await api(`/api/reviews/${el.dataset.id}`, { method: "DELETE" });
      toast("Đã xóa bài review");
      await render();
      return;
    }
    if (action === "moderate") {
      const choice = el.dataset.choice;
      if ((choice === "Delete" || choice === "Penalize") && !confirm(choice === "Delete" ? "Xóa bài review này?" : "Ẩn bài và khóa tác giả?")) return;
      const result = await api(`/api/admin/reports/${el.dataset.id}/moderate`, { method: "POST", body: { action: choice } });
      toast(result.message);
      await render();
      return;
    }
    if (action === "lock") {
      const isLocked = el.dataset.locked !== "true";
      await api(`/api/admin/users/${el.dataset.id}/lock`, { method: "PATCH", body: { isLocked } });
      toast(isLocked ? "Đã khóa tài khoản" : "Đã mở khóa");
      await render();
      return;
    }
    if (action === "delete-category") {
      if (!confirm("Xóa ngành hàng này?")) return;
      await api(`/api/admin/categories/${el.dataset.id}`, { method: "DELETE" });
      state.categories = [];
      toast("Đã xóa ngành hàng");
      await render();
    }
  } catch (error) {
    toast(error.message);
  }
});

document.body.addEventListener("submit", async (event) => {
  const form = event.target.closest("form[data-form]");
  if (!form) return;
  event.preventDefault();
  const data = new FormData(form);
  const kind = form.dataset.form;
  try {
    if (kind === "search") {
      go(`/?keyword=${encodeURIComponent(data.get("keyword") || "")}`);
      return;
    }
    if (kind === "pick-product") {
      const keyword = data.get("keyword") || "";
      go(keyword ? `/write?keyword=${encodeURIComponent(keyword)}` : "/write");
      return;
    }
    if (kind === "new-product") {
      const created = await api("/api/products", {
        method: "POST",
        body: {
          name: data.get("name"),
          brand: data.get("brand"),
          description: data.get("description") || null,
          price: Number(data.get("price") || 0),
          categoryId: Number(data.get("categoryId"))
        }
      });
      toast("Đã thêm sản phẩm. Viết đánh giá bên dưới.");
      go(`/write?productId=${created.id}`);
      return;
    }
    if (kind === "filters") {
      const q = route().query;
      q.set("minPrice", data.get("minPrice") || "");
      q.set("maxPrice", data.get("maxPrice") || "");
      q.set("sort", data.get("sort") || "newest");
      q.set("page", "1");
      ["minPrice", "maxPrice"].forEach(key => { if (!q.get(key)) q.delete(key); });
      go(`/?${q.toString()}`);
      return;
    }
    if (kind === "login" || kind === "register") {
      const payload = kind === "register"
        ? { fullName: data.get("fullName"), email: data.get("email"), password: data.get("password") }
        : { email: data.get("email"), password: data.get("password") };
      const result = await api(`/api/auth/${kind === "register" ? "register" : "login"}`, { method: "POST", body: payload });
      setSession(result.token);
      state.user = await loadMe();
      await refreshSocial();
      toast(`Xin chào ${state.user.fullName}`);
      go("/");
      return;
    }
    if (kind === "review") {
      const rating = Number(data.get("rating"));
      if (rating < 1) throw new Error("Hãy chọn số sao.");
      const files = data.getAll("files").filter(file => file && file.size);
      if (files.length) {
        const body = new FormData();
        files.forEach(file => body.append("files", file));
        const uploaded = await api("/api/reviews/uploads", { method: "POST", body });
        state.images.push(...uploaded.urls);
      }
      const reviewId = data.get("reviewId");
      const payload = {
        pros: data.get("pros"),
        cons: data.get("cons"),
        rating,
        isAnonymous: data.get("isAnonymous") === "on",
        imageUrls: state.images
      };
      if (reviewId) {
        await api(`/api/reviews/${reviewId}`, { method: "PUT", body: payload });
        toast("Đã cập nhật bài review");
      } else {
        payload.productId = Number(data.get("productId"));
        await api("/api/reviews", { method: "POST", body: payload });
        toast("Đã đăng bài review");
      }
      go(`/product/${data.get("productId")}`);
      return;
    }
    if (kind === "comment") {
      await api(`/api/social/reviews/${data.get("reviewId")}/comments`, {
        method: "POST",
        body: { content: data.get("content"), isAnonymous: data.get("isAnonymous") === "on" }
      });
      await toggleComments(data.get("reviewId"), true);
      return;
    }
    if (kind === "report") {
      const files = data.getAll("files").filter(file => file && file.size);
      let evidenceImageUrls = [];
      if (files.length) {
        const body = new FormData();
        files.forEach(file => body.append("files", file));
        const uploaded = await api("/api/social/uploads", { method: "POST", body });
        evidenceImageUrls = uploaded.urls;
      }
      await api("/api/social/reports", {
        method: "POST",
        body: { reviewId: Number(data.get("reviewId")), reason: data.get("reason"), evidenceImageUrls }
      });
      closeModal();
      toast("Đã gửi báo cáo, đang chờ kiểm duyệt");
      return;
    }
    if (kind === "user-search") {
      go(`/admin?tab=users&keyword=${encodeURIComponent(data.get("keyword") || "")}`);
      return;
    }
    if (kind === "category") {
      await api("/api/admin/categories", { method: "POST", body: { name: data.get("name"), description: data.get("description") } });
      state.categories = [];
      toast("Đã thêm ngành hàng");
      await render();
    }
  } catch (error) {
    showFormError(error.message);
  }
});

document.body.addEventListener("change", async (event) => {
  const input = event.target;
  if (input.name !== "files" || !document.getElementById("image-list")) return;
  const files = [...input.files];
  if (!files.length) return;
  try {
    const body = new FormData();
    files.forEach(file => body.append("files", file));
    const uploaded = await api("/api/reviews/uploads", { method: "POST", body });
    state.images.push(...uploaded.urls);
    document.getElementById("image-list").innerHTML = state.images.map(imageChip).join("");
    input.value = "";
    toast("Đã tải ảnh");
  } catch (error) {
    toast(error.message);
  }
});

async function toggleComments(reviewId, forceOpen = false) {
  const box = document.getElementById(`comments-${reviewId}`);
  if (!box) return;
  if (box.dataset.open === "1" && !forceOpen) {
    box.hidden = true;
    box.dataset.open = "0";
    return;
  }
  box.hidden = false;
  const data = await api(`/api/social/reviews/${reviewId}/comments?pageSize=20`);
  const items = data.items.map(comment => `<div class="comment"><strong>${esc(comment.authorName)}</strong> <span class="muted">${when(comment.createdAt)}</span><div>${esc(comment.content)}</div></div>`).join("");
  const form = state.user ? `<form class="inline-form" data-form="comment" style="margin-top:8px">
      <input type="hidden" name="reviewId" value="${reviewId}" />
      <input name="content" placeholder="Viết bình luận" required />
      <label class="row"><input type="checkbox" name="isAnonymous" /> Ẩn danh</label>
      <button class="btn" type="submit">Gửi</button>
    </form>` : "";
  box.innerHTML = `${items || `<p class="muted">Chưa có bình luận.</p>`}${form}<div id="form-error"></div>`;
  box.dataset.open = "1";
}

window.addEventListener("hashchange", () => { closeModal(); render(); });
boot();
