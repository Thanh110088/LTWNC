import { api } from "./api.js";

export const state = {
  user: null,
  categories: [],
  bookmarks: new Set(),
  following: new Set(),
  images: []
};

export async function ensureCategories() {
  if (state.categories.length === 0) {
    state.categories = await api("/api/products/categories");
  }
  return state.categories;
}

export async function refreshSocial() {
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
