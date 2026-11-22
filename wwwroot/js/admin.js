import {api} from "./api.js";
import {confirmAction, empty, esc, go, notify, pageOf, pager, url} from "./ui.js";

const roles = {Candidate:"Ứng viên", Employer:"Nhà tuyển dụng", Admin:"Quản trị viên"};

export async function adminUsersPage(ctx) {
    const route = "/admin/users";
    const keyword = ctx.query.get("keyword") || "";
    const page = pageOf(ctx.query);
    const result = await api(url("/admin/users", {keyword, page, pageSize:10}), {signal:ctx.signal});
    ctx.signal.throwIfAborted();
    const last = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    if (page > last) {
        location.replace("#" + url(route, {keyword,page:last}));
        return;
    }

    const rows = result.items.map(account => `<article class="application-card">
        <div><h3>${esc(account.fullName)}</h3><p class="metadata">${esc(account.email)} · ${roles[account.role]}</p></div>
        <div class="actions"><span class="badge ${account.isLocked ? "closed" : "open"}">${account.isLocked ? "Đã khóa" : "Hoạt động"}</span>
        ${account.role === "Admin" ? "" : `<button type="button" class="secondary small" data-account="${account.id}">${account.isLocked ? "Mở khóa" : "Khóa"}</button>`}</div></article>`).join("");
    ctx.show(`<header class="page-intro"><h1>Tài khoản người dùng</h1></header>
        <form class="actions" id="account-search" role="search"><label class="field"><span class="sr-only">Tên hoặc email</span>
        <input type="search" name="keyword" placeholder="Tìm theo tên hoặc email" maxlength="254" value="${esc(keyword)}"></label>
        <button type="submit" class="primary">Tìm kiếm</button></form>
        <div class="toolbar"><span class="count">${result.totalCount} tài khoản</span></div>
        <div class="application-list" id="user-list">${rows || empty("Không tìm thấy tài khoản", "Hãy thử tên hoặc địa chỉ email khác.")}</div>
        ${pager(result,route,{keyword})}`);
    document.querySelector("#account-search").onsubmit = event => {
        event.preventDefault();
        go(url(route, {keyword:event.currentTarget.elements.keyword.value.trim()}));
    };
    document.querySelector("#user-list").onclick = async event => {
        const button = event.target.closest("button[data-account]");
        if (!button) return;
        const account = result.items.find(u => u.id === Number(button.dataset.account));
        const action = account.isLocked ? "Mở khóa" : "Khóa";
        if (!await confirmAction(action + " tài khoản?", account.email, action) || ctx.signal.aborted) return;
        button.disabled = true;
        try {
            await api(`/admin/users/${account.id}/lock`,{method:"PATCH",body:{isLocked:!account.isLocked}});
            if (!ctx.signal.aborted) { notify("Đã cập nhật trạng thái tài khoản."); go(url(route,{keyword,page})); }
        } catch (error) {
            if (!ctx.signal.aborted) notify(error.message);
        } finally { button.disabled = false; }
    };
}
