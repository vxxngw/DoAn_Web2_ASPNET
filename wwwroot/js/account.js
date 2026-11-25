import { api, setSession } from "./api.js";
import { bindForm, errorBox, esc, go, internalNext, notify, url } from "./ui.js";

export function accountPage(ctx, register) {
    const role = ctx.query.get("role") === "Employer" ? "Employer" : "Candidate";
    const next = internalNext(ctx.query.get("next"), "");
    const switchUrl = url(register ? "/login" : "/register", {next, role});
    const extra = register ? `<label class="field">Loại tài khoản<select name="role">
      <option value="Candidate">Ứng viên</option><option value="Employer" ${role === "Employer" ? "selected" : ""}>Nhà tuyển dụng</option></select></label>
      <label class="field">Họ và tên<input name="fullName" required maxlength="100" autocomplete="name"></label>
      <label class="field" id="company-field">Tên công ty<input name="companyName" maxlength="150" autocomplete="organization"></label>` : "";
    ctx.show(`<div class="auth-view"><section class="auth-left">
      <a class="brand" href="#/"><img src="/assets/jobpilot/brand.svg" alt="">Việc Mở</a>
      <div class="auth-form"><h1>${register ? "Tạo tài khoản" : "Đăng nhập"}</h1>
      <p class="muted">${register ? "Đã có tài khoản?" : "Chưa có tài khoản?"}
      <a href="#${esc(switchUrl)}">${register ? "Đăng nhập" : "Đăng ký"}</a></p>
      <form id="account-form" class="stack">${extra}
      <label class="field">Email<input type="email" name="email" required maxlength="254" autocomplete="email" placeholder="Địa chỉ email"></label>
      <label class="field">Mật khẩu<input type="password" name="password" required maxlength="128" ${register ? 'minlength="8" autocomplete="new-password"' : 'autocomplete="current-password"'} placeholder="Nhập mật khẩu">
      ${register ? '<span class="hint">Tối thiểu 8 ký tự.</span>' : ""}</label>${errorBox}
      <button class="primary" type="submit">${register ? "Tạo tài khoản" : "Đăng nhập"}<img src="/assets/jobpilot/arrow.svg" alt=""></button>
      ${!register ? `<p class="muted"><a href="#/forgot-password">Quên mật khẩu?</a></p>` : ""}
      </form>
      </div></section><aside class="auth-visual"><div><h2>${role === "Employer" ? "Kết nối với ứng viên phù hợp." : "Tìm cơ hội nghề nghiệp tiếp theo."}</h2>
      <p>${role === "Employer" ? "Đăng tin tuyển dụng và quản lý hồ sơ ứng viên trên Việc Mở." : "Tìm việc làm, gửi CV và theo dõi kết quả ứng tuyển trên Việc Mở."}</p></div></aside></div>`);
    const form = document.querySelector("#account-form");
    if (register) {
        const showCompany = () => {
            const employer = form.elements.role.value === "Employer";
            document.querySelector("#company-field").hidden = !employer;
            form.elements.companyName.required = employer;
            form.elements.companyName.disabled = !employer;
        };
        form.elements.role.onchange = showCompany;
        showCompany();
    }
    bindForm("account-form", async data => {
        const fields = Object.fromEntries(data);
        if (register) await api("/auth/register",{method:"POST",body:fields,anonymous:true});
        const login = await api("/auth/login",{method:"POST",body:{email:fields.email,password:fields.password},anonymous:true});
        if (ctx.signal.aborted) return;
        setSession(login);
        const fallback = login.user.role === "Admin" ? "/admin/users" : login.user.role === "Employer" ? "/employer" : "/jobs";
        go(next || fallback);
        notify(register ? "Tạo tài khoản thành công." : "Đã đăng nhập.");
    });
}

export function forgotPasswordPage(ctx) {
    ctx.show(`<div class="auth-view"><section class="auth-left">
      <a class="brand" href="#/"><img src="/assets/jobpilot/brand.svg" alt="">Việc Mở</a>
      <div class="auth-form"><h1>Quên mật khẩu</h1>
      <p class="muted">Nhập email để tạo liên kết đặt lại mật khẩu.</p>
      <form id="forgot-form" class="stack">
      <label class="field">Email<input type="email" name="email" required maxlength="254" autocomplete="email" placeholder="Địa chỉ email"></label>
      ${errorBox}
      <button class="primary" type="submit">Tạo liên kết đặt lại<img src="/assets/jobpilot/arrow.svg" alt=""></button></form>
      <p class="muted"><a href="#/login">Quay lại đăng nhập</a></p></div></section>
      <aside class="auth-visual"><div><h2>Khôi phục tài khoản.</h2><p>Tạo mật khẩu mới để tiếp tục sử dụng Việc Mở.</p></div></aside></div>`);
    bindForm("forgot-form", async data => {
        const result = await api("/auth/forgot-password", {method:"POST", body:Object.fromEntries(data), anonymous:true});
        if (ctx.signal.aborted) return;
        if (result.resetToken) {
            go("/reset-password?token=" + encodeURIComponent(result.resetToken));
            notify("Liên kết đặt lại mật khẩu đã được tạo.");
        } else {
            notify(result.message);
        }
    });
}

export function resetPasswordPage(ctx) {
    const token = ctx.query.get("token") || "";
    ctx.show(`<div class="auth-view"><section class="auth-left">
      <a class="brand" href="#/"><img src="/assets/jobpilot/brand.svg" alt="">Việc Mở</a>
      <div class="auth-form"><h1>Đặt lại mật khẩu</h1>
      <p class="muted">Nhập mật khẩu mới cho tài khoản.</p>
      <form id="reset-form" class="stack">
      <label class="field">Mật khẩu mới<input type="password" name="newPassword" required minlength="8" maxlength="128" autocomplete="new-password" placeholder="Tối thiểu 8 ký tự"></label>
      ${errorBox}
      <button class="primary" type="submit">Cập nhật mật khẩu<img src="/assets/jobpilot/arrow.svg" alt=""></button></form>
      <p class="muted"><a href="#/login">Quay lại đăng nhập</a></p></div></section>
      <aside class="auth-visual"><div><h2>Khôi phục tài khoản.</h2><p>Mật khẩu mới sẽ được mã hóa an toàn trước khi lưu.</p></div></aside></div>`);
    bindForm("reset-form", async data => {
        const fields = Object.fromEntries(data);
        await api("/auth/reset-password", {method:"POST", body:{token, newPassword:fields.newPassword}, anonymous:true});
        if (ctx.signal.aborted) return;
        go("/login");
        notify("Đặt lại mật khẩu thành công. Hãy đăng nhập lại.");
    });
}
