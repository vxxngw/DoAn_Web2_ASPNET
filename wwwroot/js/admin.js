import {api} from "./api.js";
import {confirmAction, empty, esc, go, notify, pageOf, pageSizeOf, pager, url} from "./ui.js";

const roles = {Candidate:"Ứng viên", Employer:"Nhà tuyển dụng", Admin:"Quản trị viên"};

export async function adminUsersPage(ctx) {
    const route = "/admin/users";
    const keyword = ctx.query.get("keyword") || "";
    const role = ctx.query.get("role") === "Employer" ? "Employer" : "Candidate";
    const page = pageOf(ctx.query);
    const pageSize = pageSizeOf(ctx.query, 10);
    const params = { keyword, role, pageSize };
    const result = await api(url("/admin/users", {...params, page}), {signal:ctx.signal});
    ctx.signal.throwIfAborted();
    const totalPages = result.totalPages ?? Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    if (page > totalPages) {
        location.replace("#" + url(route, {...params, page: totalPages}));
        return;
    }

    const rows = result.items.map(account => `<article class="application-card">
        <div><h3>${esc(account.role === "Employer" && account.companyName ? account.companyName : account.fullName)}</h3><p class="metadata">${esc(account.email)} · ${roles[account.role]}${account.role === "Employer" && account.companyName ? ` · ${esc(account.fullName)}` : ""}</p></div>
        <div class="actions"><span class="badge ${account.isLocked ? "closed" : "open"}">${account.isLocked ? "Đã khóa" : "Hoạt động"}</span>
        ${account.role === "Admin" ? "" : `<button type="button" class="secondary small" data-action="lock" data-account="${account.id}">${account.isLocked ? "Mở khóa" : "Khóa"}</button>
        <button type="button" class="danger small" data-action="delete" data-account="${account.id}">Xóa</button>`}</div></article>`).join("");
    const pageSubtitle = totalPages > 1 ? ` · Trang ${result.page}/${totalPages}` : "";
    ctx.show(`<header class="page-intro"><h1>${role === "Employer" ? "Tài khoản nhà tuyển dụng" : "Tài khoản ứng viên"}</h1></header>
        <div class="chips" aria-label="Loại tài khoản" style="margin-bottom:18px;">
          <a class="chip ${role === "Candidate" ? "current" : ""}" href="#/admin/users?role=Candidate">Ứng viên</a>
          <a class="chip ${role === "Employer" ? "current" : ""}" href="#/admin/users?role=Employer">Nhà tuyển dụng</a>
        </div>
        <form class="actions" id="account-search" role="search"><label class="field"><span class="sr-only">Tên hoặc email</span>
        <input type="search" name="keyword" placeholder="Tìm theo tên hoặc email" maxlength="254" value="${esc(keyword)}"></label>
        <button type="submit" class="primary">Tìm kiếm</button></form>
        <div class="toolbar"><span class="count">${result.totalCount} tài khoản${pageSubtitle}</span></div>
        <div class="application-list" id="user-list">${rows || empty("Không tìm thấy tài khoản", "Hãy thử tên hoặc địa chỉ email khác.")}</div>
        ${pager(result,route,params)}`);
    document.querySelector("#account-search").onsubmit = event => {
        event.preventDefault();
        go(url(route, {...params, keyword:event.currentTarget.elements.keyword.value.trim(), page: 1}));
    };
    document.querySelector("#user-list").onclick = async event => {
        const button = event.target.closest("button[data-account]");
        if (!button) return;
        const account = result.items.find(u => u.id === Number(button.dataset.account));
        const deleting = button.dataset.action === "delete";
        const action = deleting ? "Xóa" : account.isLocked ? "Mở khóa" : "Khóa";
        const message = deleting
            ? account.role === "Employer"
                ? `${account.email}. Thao tác này sẽ xóa tài khoản, tất cả tin tuyển dụng và hồ sơ ứng tuyển liên quan.`
                : `${account.email}. Ứng viên có hồ sơ ứng tuyển liên quan sẽ không thể xóa.`
            : account.email;
        if (!await confirmAction(action + " tài khoản?", message, action) || ctx.signal.aborted) return;
        button.disabled = true;
        try {
            if (deleting) {
                await api(`/admin/users/${account.id}`,{method:"DELETE"});
            } else {
                await api(`/admin/users/${account.id}/lock`,{method:"PATCH",body:{isLocked:!account.isLocked}});
            }
            if (!ctx.signal.aborted) {
                notify(deleting ? "Đã xóa tài khoản." : "Đã cập nhật trạng thái tài khoản.");
                go(url(route,{...params, page}));
            }
        } catch (error) {
            if (!ctx.signal.aborted) notify(error.message);
        } finally { button.disabled = false; }
    };
}
