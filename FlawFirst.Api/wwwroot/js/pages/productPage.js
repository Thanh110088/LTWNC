import { api } from "../api.js";
import { esc, vnd, stars, thumb, hashWith, route } from "../utils.js";
import { pager } from "../components/pager.js";
import { reviewCard } from "../components/reviewCard.js";

export async function productPage(id) {
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
