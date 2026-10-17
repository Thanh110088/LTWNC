import { esc, stars, thumb, vnd } from "../utils.js";

export function productCard(product) {
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
