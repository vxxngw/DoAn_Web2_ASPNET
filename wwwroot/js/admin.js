import {api} from "./api.js";
import {back, bindForm, confirmAction, errorBox, esc, go, notify, pageOf, pager, url} from "./ui.js";

const roles = {Candidate:"Ứng viên", Employer:"Nhà tuyển dụng", Admin:"Quản trị viên"};
const usersRoute = "/admin/users";

function accountTable(role, result, params, pageKey) {
    const employer = role === "Employer";
    const title = roles[role];
    const columns = employer ? ["Công ty", "Người liên hệ", "Email", "Trạng thái", "Thao tác"]
        : ["Họ và tên", "Email", "Trạng thái", "Thao tác"];
    const rows = result.items.map(account => `<tr>
        <th scope="row">${esc(employer ? account.companyName || account.fullName : account.fullName)}</th>
        ${employer ? `<td class="contact-cell"><span class="mobile-label">Liên hệ · </span>${esc(account.fullName)}</td>` : ""}
        <td class="email-cell">${esc(account.email)}</td>
        <td class="status-cell"><span class="badge ${account.isLocked ? "closed" : "open"}">${account.isLocked ? "Đã khóa" : "Hoạt động"}</span></td>
        <td class="action-cell"><div class="actions">
          <a class="secondary small" href="#${esc(url(`/admin/users/${account.id}/edit`, params))}">Sửa</a>
          <button type="button" class="secondary small" data-account="${account.id}">${account.isLocked ? "Mở khóa" : "Khóa"}</button>
          <button type="button" class="danger small" data-delete="${account.id}">Xóa</button>
        </div></td></tr>`).join("");
    return `<section class="account-section" aria-labelledby="heading-${role}">
        <header><h2 id="heading-${role}">${title}</h2><span class="count">${result.totalCount} tài khoản</span></header>
        <table class="account-table" aria-labelledby="heading-${role}">
          <thead><tr>${columns.map(col => `<th scope="col">${col}</th>`).join("")}</tr></thead>
          <tbody>${rows || `<tr><td class="account-empty" colspan="${columns.length}">${params.keyword ? "Không có tài khoản khớp với từ khóa." : "Chưa có tài khoản."}</td></tr>`}</tbody></table>
        ${pager(result,usersRoute,params,pageKey,`Phân trang ${title}`)}</section>`;
}

export async function adminUsersPage(ctx) {
    const keyword = ctx.query.get("keyword") || "";
    const candidatePage = pageOf(ctx.query, ctx.query.has("candidatePage") ? "candidatePage" : "page");
    const employerPage = pageOf(ctx.query, "employerPage");
    const params = {keyword, candidatePage, employerPage};
    const load = (role, page) => api(url(usersRoute, {role, keyword, page, pageSize:5}), {signal:ctx.signal});
    const [candidates, employers] = await Promise.all([load("Candidate", candidatePage), load("Employer", employerPage)]);
    ctx.signal.throwIfAborted();
    const last = result => Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    if (candidatePage > last(candidates) || employerPage > last(employers)) {
        location.replace("#" + url(usersRoute, {keyword, candidatePage:Math.min(candidatePage,last(candidates)),
            employerPage:Math.min(employerPage,last(employers))}));
        return;
    }

    const accounts = [...candidates.items, ...employers.items];
    ctx.show(`<header class="page-intro"><h1>Tài khoản người dùng</h1></header>
        <form class="actions" id="account-search" role="search"><label class="field"><span class="sr-only">Tên, email hoặc công ty</span>
        <input type="search" name="keyword" placeholder="Tìm theo tên, email hoặc công ty" maxlength="254" value="${esc(keyword)}"></label>
        <button type="submit" class="primary">Tìm kiếm</button></form>
        <div id="user-list">${accountTable("Candidate", candidates, params, "candidatePage")}
        ${accountTable("Employer", employers, params, "employerPage")}</div>`);
    document.querySelector("#account-search").onsubmit = event => {
        event.preventDefault();
        go(url(usersRoute, {keyword:event.currentTarget.elements.keyword.value.trim()}));
    };
    document.querySelector("#user-list").onclick = async event => {
        const remove = event.target.closest("button[data-delete]");
        if (remove) {
            const account = accounts.find(u => u.id === Number(remove.dataset.delete));
            remove.disabled = true;
            try {
                const path = `/admin/users/${account.id}`;
                const impact = await api(path + "/deletion-impact", {signal:ctx.signal});
                const warning = `${account.email} và dữ liệu liên quan sẽ bị xóa vĩnh viễn, gồm ${impact.jobs} tin và ${impact.applications} đơn kèm CV.`;
                if (!await confirmAction("Xóa tài khoản?", warning, "Xóa vĩnh viễn") || ctx.signal.aborted) return;
                await api(path, {method:"DELETE"});
                if (!ctx.signal.aborted) { notify("Đã xóa tài khoản."); go(url(usersRoute,params)); }
            } catch (error) {
                if (!ctx.signal.aborted) notify(error.message);
            } finally { remove.disabled = false; }
            return;
        }
        const button = event.target.closest("button[data-account]");
        if (!button) return;
        const account = accounts.find(u => u.id === Number(button.dataset.account));
        const action = account.isLocked ? "Mở khóa" : "Khóa";
        if (!await confirmAction(action + " tài khoản?", account.email, action) || ctx.signal.aborted) return;
        button.disabled = true;
        try {
            await api(`/admin/users/${account.id}/lock`,{method:"PATCH",body:{isLocked:!account.isLocked}});
            if (!ctx.signal.aborted) { notify("Đã cập nhật trạng thái tài khoản."); go(url(usersRoute,params)); }
        } catch (error) {
            if (!ctx.signal.aborted) notify(error.message);
        } finally { button.disabled = false; }
    };
}

export async function adminUserForm(ctx, id) {
    const path = `/admin/users/${id}`;
    const account = await api(path, {signal:ctx.signal});
    if (account.role === "Admin") throw new Error("Chỉ có thể sửa ứng viên và nhà tuyển dụng.");
    const isEmployer = account.role === "Employer";
    const listPath = url(usersRoute,{keyword:ctx.query.get("keyword"), candidatePage:pageOf(ctx.query,"candidatePage"),
        employerPage:pageOf(ctx.query,"employerPage")});
    ctx.show(`<div class="narrow">${back(listPath,"Tài khoản người dùng")}
        <section class="panel stack"><h1>Sửa tài khoản</h1><p class="metadata">${roles[account.role]}</p>
        <form id="edit-account" class="stack"><label class="field">Họ tên<input name="fullName" required maxlength="100" value="${esc(account.fullName)}"></label>
        <label class="field">Email đăng nhập<input type="email" name="email" required maxlength="254" value="${esc(account.email)}"></label>
        ${isEmployer ? `<label class="field">Tên công ty<input name="companyName" required maxlength="150" value="${esc(account.companyName)}"></label>` : ""}
        ${errorBox}<div class="actions"><button type="submit" class="primary">Lưu thay đổi</button>
        <a class="secondary" href="#${esc(listPath)}">Hủy</a></div></form></section></div>`);
    bindForm("edit-account", async data => {
        await api(path, {method:"PUT", body:Object.fromEntries(data)});
        if (!ctx.signal.aborted) { notify("Đã cập nhật tài khoản."); go(url("/admin/users",{keyword:data.get("email").trim()})); }
    });
}

const periods = {Day:"Ngày", Week:"Tuần", Month:"Tháng", All:"Toàn thời gian"};

export async function adminStatisticsPage(ctx) {
    const period = ctx.query.get("period") || "Day";
    const date = ctx.query.get("date");
    const report = await api(url("/admin/statistics", {period,date}), {signal:ctx.signal});
    const selection = date || new Intl.DateTimeFormat("en-CA",{timeZone:"Asia/Ho_Chi_Minh"}).format(new Date());
    const formatDate = value => value.split("-").reverse().join("/");
    const range = report.period === "All" ? "Toàn bộ thời gian" : report.period === "Day"
        ? formatDate(report.startDate) : `${formatDate(report.startDate)} – ${formatDate(report.endDateInclusive)}`;
    const metrics = [["Tin đăng",report.jobsPosted], ["Đơn đã nộp",report.applicationsSubmitted],
        ["Đơn được chấp nhận",report.accepted], ["Đơn bị từ chối",report.rejected]];
    const cards = metrics.map(([label,value]) => `<div><dt>${label}</dt><dd>${value.toLocaleString("vi-VN")}</dd></div>`).join("");
    const route = "/admin/statistics";
    const filters = Object.entries(periods).map(([key,label]) =>
        `<a class="chip ${report.period === key ? "current" : ""}" ${report.period === key ? 'aria-current="page"' : ""}
            href="#${esc(url(route,{period:key,date:key === "All" ? null : selection}))}">${label}</a>`).join("");
    ctx.show(`<header class="page-intro"><h1>Thống kê tuyển dụng</h1></header>
      <nav class="chips" aria-label="Kỳ thống kê">${filters}</nav>
      ${report.period === "All" ? "" : `<form id="report-date" class="actions toolbar"><label class="field">Ngày trong kỳ
        <input type="date" name="date" required value="${esc(selection)}"></label><button class="secondary" type="submit">Xem báo cáo</button></form>`}
      <section class="report-section"><h2>${range}</h2><p class="metadata">Giờ Việt Nam (UTC+7) · Tuần bắt đầu từ thứ Hai.</p>
        <dl class="report-metrics">${cards}</dl>
        <p class="hint">Tin tính theo ngày đăng, đơn theo ngày nộp, kết quả theo ngày xét duyệt.</p>
        ${report.reviewsWithoutDate ? `<p class="hint">Có ${report.reviewsWithoutDate} kết quả cũ chưa có ngày xét duyệt, chỉ tính ở kỳ toàn thời gian.</p>` : ""}
      </section>`);
    const form = document.querySelector("#report-date");
    if (form) form.onsubmit = event => {
        event.preventDefault();
        go(url(route,{period:report.period,date:form.elements.date.value}));
    };
}
