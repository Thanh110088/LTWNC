import { api } from "../api.js";
import { state } from "../state.js";
import { esc } from "../utils.js";

export async function userPage(id) {
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
