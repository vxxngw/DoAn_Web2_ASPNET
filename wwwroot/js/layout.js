import {user} from "./api.js";
import {esc, icon} from "./ui.js";

export function art(name) {
    return `<img src="/assets/jobpilot/${name}.svg" alt="" class="design-icon">`;
}

export function breadcrumb(title, parent, heading = false) {
    return `<div class="breadcrumb"><div class="wrap">
      <${heading ? "h1" : "strong"}>${esc(title)}</${heading ? "h1" : "strong"}><nav aria-label="Đường dẫn"><a href="#/">Trang chủ</a>
      ${parent ? `<span>/</span><a href="#${esc(parent[0])}">${esc(parent[1])}</a>` : ""}
      <span>/</span><span aria-current="page">${esc(title)}</span></nav></div></div>`;
}

export function dashboard(content, path) {
    const employer = user.role === "Employer";
    const admin = user.role === "Admin";
    const links = admin ? [["/admin/users","file","Tài khoản người dùng"],["/admin/jobs","work","Tin tuyển dụng"]] : employer ? [["/employer","work","Tin của tôi"],["/employer/new","plus","Đăng tin mới"]] :
        [["/applications","file","Đơn đã nộp"]];
    const nav = links.map(([target, symbol, label]) =>
        `<a href="#${target}" ${path === target ? 'aria-current="page"' : ""}>${icon(symbol)}${label}</a>`).join("");
    return `<div class="wrap dashboard"><aside class="dashboard-nav">
      <p>${admin ? "Quản trị viên" : employer ? "Nhà tuyển dụng" : "Ứng viên"}</p><nav aria-label="Quản lý tài khoản">${nav}</nav>
      </aside><div class="dashboard-body">${content}</div></div>`;
}
