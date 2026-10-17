export function authPage(isRegister) {
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
