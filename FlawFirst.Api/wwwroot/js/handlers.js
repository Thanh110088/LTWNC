import { api, clearSession, loadMe, setSession } from "./api.js";
import { state, refreshSocial } from "./state.js";
import { hashWith, starSvg } from "./utils.js";
import { toast, openModal, closeModal, showFormError } from "./ui.js";
import { imageChip } from "./components/reviewCard.js";
import { toggleComments } from "./components/comments.js";

export function initHandlers({ render, go }) {
  document.body.addEventListener("click", async (event) => {
    if (event.target.closest(".modal-back") && event.target.classList.contains("modal-back")) {
      closeModal();
    }

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
        const result = await api(`/api/social/reviews/${el.dataset.id}/votes`, {
          method: "POST",
          body: { voteType: el.dataset.vote }
        });
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
      if (action === "close") {
        closeModal();
        return;
      }
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
        const q = new URLSearchParams(location.hash.split("?")[1] || "");
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
}
