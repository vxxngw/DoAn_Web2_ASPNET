import {api, setSession, setUser} from "./api.js";
import {bindCandidateProfile, candidateSections} from "./profile.js";
import {bindForm, errorBox, esc, go, icon, internalNext, notify, url} from "./ui.js";

const roleNames = {Admin:"Quản trị viên", Candidate:"Ứng viên", Employer:"Nhà tuyển dụng"};
const overviewPaths = {Admin:"/admin/statistics", Candidate:"/applications", Employer:"/employer"};

function portrait(account) {
    if (account.avatarDataUrl)
        return `<span class="personal-avatar"><img alt="" src="${esc(account.avatarDataUrl)}"></span>`;
    const words = account.fullName.trim().split(/\s+/);
    const initials = (words.length > 1 ? words[0][0] + words.at(-1)[0] : words[0][0] || "?").toLocaleUpperCase("vi");
    return `<span class="personal-avatar tone-${account.id % 6}" aria-hidden="true">${esc(initials)}</span>`;
}

export function profileMenu(account) {
    return `<details class="profile-menu"><summary class="profile-trigger" aria-label="Menu tài khoản của ${esc(account.fullName)}">
        ${portrait(account)}${icon("down")}</summary>
        <nav class="profile-dropdown" aria-label="Menu cá nhân">
          <div class="profile-identity"><strong>${esc(account.fullName)}</strong><span>${esc(account.email)}</span></div>
          <a href="#${overviewPaths[account.role]}">${icon("overview")}Tổng Quan</a>
          <a href="#/profile">${icon("person")}Hồ sơ và tài khoản</a>
          <button id="logout" type="button">${icon("logout")}Đăng Xuất</button>
        </nav></details>`;
}

export async function personalProfilePage(ctx) {
    const account = await api("/auth/me",{signal:ctx.signal});
    let candidate, profileError;
    if (account.role === "Candidate") {
        try { candidate = await api("/profile", {signal:ctx.signal}); }
        catch (error) {
            if (ctx.signal.aborted || error.status === 401) throw error;
            profileError = error.message;
        }
    }
    ctx.show(`<div class="narrow"><header class="page-intro"><h1>Hồ sơ và tài khoản</h1></header>
      <section class="panel stack personal-profile"><div class="profile-heading">${portrait(account)}<div>
        <h2>${esc(account.fullName)}</h2><p class="metadata">${roleNames[account.role]}</p></div></div>
        <dl><dt>Email đăng nhập</dt><dd>${esc(account.email)}</dd>
        ${account.companyName ? `<dt>Công ty</dt><dd>${esc(account.companyName)}</dd>` : ""}</dl>
        <div class="actions"><button type="button" class="secondary" aria-haspopup="dialog" aria-controls="personal-dialog">Sửa thông tin</button>
          <button type="button" class="secondary" aria-haspopup="dialog" aria-controls="password-dialog">Đổi mật khẩu</button></div>
        <form class="stack avatar-form" id="avatar-form"><label class="field">Đổi ảnh đại diện
          <input type="file" name="image" required accept="image/jpeg,image/png" aria-describedby="avatar-hint">
          <span id="avatar-hint" class="hint">Chọn ảnh JPG hoặc PNG, tối đa 5 MiB. Ảnh sẽ được cắt vuông ở giữa.</span></label>
          ${errorBox}<div><button type="submit" class="primary">Lưu ảnh</button></div></form>
      </section>${accountDialogs(account)}${candidate ? candidateSections(account, candidate) : ""}
      ${profileError ? `<div class="panel stack"><p role="alert">Chưa tải được hồ sơ ứng viên. ${esc(profileError)}</p>
          <div><button type="button" class="secondary" id="retry-profile">Thử lại</button></div></div>` : ""}
      ${account.role === "Employer" ? `<form id="company-logo-form" class="panel stack company-logo-form" aria-labelledby="logo-title">
        <h2 id="logo-title">Logo công ty</h2>
        ${account.hasCompanyLogo ? `<img class="company-logo" src="/api/companies/${account.id}/logo" alt="Logo hiện tại của ${esc(account.companyName)}">` : ""}
        <label class="field">Chọn logo<input type="file" name="image" required accept="image/jpeg,image/png" aria-describedby="logo-hint"></label>
        <span class="hint" id="logo-hint">Chọn ảnh JPG hoặc PNG, tối đa 1 MiB. Logo hiển thị trên tin tuyển dụng và thẻ công ty.</span>
        ${errorBox}<div><button type="submit" class="primary">Lưu logo</button></div></form>` : ""}</div>`);
    bindAccountDialogs(ctx);
    if (profileError) document.getElementById("retry-profile").onclick = () => go(location.hash.slice(1));
    if (account.role === "Employer") bindForm("company-logo-form", async data => {
        const file = data.get("image");
        if (file.size > 1024 * 1024) throw new Error("Logo công ty vượt quá 1 MiB.");
        await api("/companies/me/logo",{method:"PUT",body:data,signal:ctx.signal});
        if (ctx.signal.aborted) return;
        go("/profile");
        notify("Đã cập nhật logo công ty.");
    });
    bindForm("avatar-form", async data => {
        const thumb = await makeThumbnail(data.get("image"));
        ctx.signal.throwIfAborted();
        const body = new FormData();
        body.append("image", thumb, "avatar.jpg");
        const updated = await api("/auth/me/avatar",{method:"PUT",body,signal:ctx.signal});
        if (ctx.signal.aborted) return;
        setUser(updated);
        go("/profile");
        notify("Đã cập nhật ảnh đại diện.");
    });
    if (candidate) bindCandidateProfile(ctx, account, candidate, ctx.query.has("setup") ? internalNext(ctx.query.get("next"), "") : null);
}

async function makeThumbnail(file) {
    if (!["image/jpeg","image/png"].includes(file.type)) throw new Error("Vui lòng chọn ảnh JPG hoặc PNG.");
    if (file.size > 5 * 1024 * 1024) throw new Error("Ảnh vượt quá 5 MiB.");
    const image = new Image();
    const src = URL.createObjectURL(file);
    try {
        image.src = src;
        await image.decode();
        const side = Math.min(image.naturalWidth, image.naturalHeight);
        const canvas = document.createElement("canvas");
        canvas.width = canvas.height = 256;
        const context = canvas.getContext("2d");
        context.fillStyle = "#fff";
        context.fillRect(0, 0, 256, 256);
        context.drawImage(image, (image.naturalWidth-side)/2, (image.naturalHeight-side)/2, side, side, 0, 0, 256, 256);
        const blob = await new Promise(resolve => canvas.toBlob(resolve,"image/jpeg",0.88));
        if (!blob) throw new Error();
        return blob;
    } catch {
        throw new Error("Không đọc được ảnh. Vui lòng chọn tệp JPG hoặc PNG khác.");
    } finally {
        URL.revokeObjectURL(src);
    }
}

function passwordField(name, label, autocomplete, attributes = "") {
    return `<div class="field">
        <label for="${name}">${label}</label>
        <div class="password-control">
          <input id="${name}" name="${name}" type="password" autocomplete="${autocomplete}" maxlength="128" required ${attributes}>
          <button type="button" class="password-toggle" aria-controls="${name}" aria-label="Hiện nội dung ô ${label}" title="Hiện nội dung ô ${label}">${icon("eye")}</button>
        </div></div>`;
}

function bindPasswordToggles(form) {
    for (const button of form.querySelectorAll(".password-toggle")) {
        const input = form.elements.namedItem(button.getAttribute("aria-controls"));
        function setVisible(visible) {
            input.type = visible ? "text" : "password";
            const action = visible ? "Ẩn" : "Hiện";
            button.title = `${action} nội dung ô ${input.labels[0].textContent}`;
            button.setAttribute("aria-label", button.title);
            button.innerHTML = icon(visible ? "eyeOff" : "eye");
        }
        button.addEventListener("click", () => setVisible(input.type === "password"));
        form.addEventListener("reset", () => setVisible(false));
    }
}

function accountDialogs(account) {
    return `<dialog id="personal-dialog" class="profile-dialog" aria-labelledby="personal-title">
        <form class="stack" id="personal-settings">
          <h2 id="personal-title">Sửa thông tin tài khoản</h2>
          <label class="field">Họ và tên<input name="fullName" required maxlength="100" value="${esc(account.fullName)}"></label>
          ${account.role === "Employer" ? `<label class="field">Tên công ty<input name="companyName" required maxlength="150" value="${esc(account.companyName)}"></label>` : ""}
          ${errorBox}<div class="actions"><button type="submit" class="primary">Lưu thay đổi</button><button type="button" class="secondary" data-close>Hủy</button></div>
        </form></dialog>
        <dialog id="password-dialog" class="profile-dialog" aria-labelledby="change-password-title">
        <form class="stack" id="change-password">
          <h2 id="change-password-title">Đổi mật khẩu</h2>
          ${passwordField("currentPassword", "Mật khẩu hiện tại", "current-password")}
          ${passwordField("newPassword", "Mật khẩu mới", "new-password", 'minlength="8" aria-describedby="password-length"')}
          <span id="password-length" class="hint">Dùng từ 8 đến 128 ký tự.</span>
          ${passwordField("confirmNewPassword", "Xác nhận mật khẩu mới", "new-password")}
          ${errorBox}<div class="actions"><button type="submit" class="primary">Đổi mật khẩu</button><button type="button" class="secondary" data-close>Hủy</button></div>
        </form></dialog>`;
}

function bindAccountDialogs(ctx) {
    for (const trigger of document.querySelectorAll(".personal-profile button[aria-controls]")) {
        const dialog = document.getElementById(trigger.getAttribute("aria-controls"));
        trigger.onclick = () => dialog.showModal();
        dialog.querySelector("[data-close]").onclick = () => dialog.close();
        dialog.addEventListener("close", () => {
            dialog.querySelector("form").reset();
            dialog.querySelector(".form-error").textContent = "";
        });
    }
    bindForm("personal-settings", async data => {
        const updated = await api("/auth/me",{method:"PUT",body:Object.fromEntries(data),signal:ctx.signal});
        if (ctx.signal.aborted) return;
        setUser(updated);
        go("/profile");
        notify("Đã cập nhật tài khoản.");
    });
    bindPasswordToggles(document.getElementById("change-password"));
    bindForm("change-password", async (data, form) => {
        const fields = Object.fromEntries(data);
        if (fields.newPassword !== fields.confirmNewPassword)
            throw new Error("Xác nhận mật khẩu mới không khớp.");
        await api("/auth/me/password",{method:"PUT",body:fields,signal:ctx.signal});
        if (ctx.signal.aborted) return;
        form.closest("dialog").close();
        notify("Đã đổi mật khẩu. Hãy dùng mật khẩu mới ở lần đăng nhập sau.");
    });
}

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
      <button class="primary" type="submit">${register ? "Tạo tài khoản" : "Đăng nhập"}<img src="/assets/jobpilot/arrow.svg" alt=""></button></form>
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
        // Ứng viên mới được đưa thẳng tới bước thiết lập hồ sơ, xong hoặc bỏ qua thì quay về trang định mở.
        go(register && login.user.role === "Candidate" ? url("/profile", {setup:1, next}) : next || fallback);
        notify(register ? "Tạo tài khoản thành công." : "Đã đăng nhập.");
    });
}
