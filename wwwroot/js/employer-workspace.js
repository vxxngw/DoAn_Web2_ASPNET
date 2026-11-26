import {api} from "./api.js";
import {back, badge, date, empty, esc, go, pageOf, pager, statuses, url} from "./ui.js";

const jobStates = {Open:"Đang tuyển", Closed:"Đã đóng", Expired:"Hết hạn", Archived:"Đã lưu trữ"};

function redirectPastLastPage(result, route, filters = {}) {
    const last = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    if (result.page <= last) return false;
    location.replace("#" + url(route, {...filters, page:last}));
    return true;
}

export async function employerReport(ctx) {
    const route = "/employer/report";
    const report = await api(url(route,{page:pageOf(ctx.query),pageSize:10}), {signal:ctx.signal});
    if (redirectPastLastPage(report.jobs,route)) return;
    const metrics = [["Đơn ứng tuyển", report.totalApplications], ...Object.entries(jobStates).map(([key,label]) => [label,report.totals[key.toLowerCase()]])];
    const rows = report.jobs.items.map(job => `<tr>
        <th scope="row"><a href="#/job/${job.id}">${esc(job.title)}</a></th>
        <td><span class="badge ${job.status === "Open" ? "open" : "closed"}">${jobStates[job.status]}</span></td>
        <td>${date(job.deadline)}</td><td><a href="#/employer/${job.id}/applications">${job.applications} đơn</a></td>
        <td>${job.status === "Archived" ? `<a href="#/employer/${job.id}/applications">Xem lịch sử</a>` : `<a href="#/employer/${job.id}/edit">Quản lý tin</a>`}</td></tr>`).join("");
    ctx.show(`<header class="section-head page-intro"><h1>Báo cáo tuyển dụng</h1><a class="secondary" href="#/employer/applicants">Xem đơn ứng tuyển</a></header>
        <dl class="report-metrics">${metrics.map(([label,value]) => `<div><dt>${label}</dt><dd>${value}</dd></div>`).join("")}</dl>
        <section class="account-section"><header><h2>Tình trạng tin tuyển dụng</h2><span class="count">${report.jobs.totalCount} tin</span></header>
        ${rows ? `<div class="report-table-scroll" tabindex="0" role="region" aria-label="Tình trạng tin tuyển dụng"><table><thead><tr><th>Vị trí</th><th>Trạng thái</th><th>Hạn nộp</th><th>Ứng tuyển</th><th>Thao tác</th></tr></thead><tbody>${rows}</tbody></table></div>` :
            empty("Chưa có tin tuyển dụng","Đăng tin đầu tiên để theo dõi đơn ứng tuyển.",'<a class="primary" href="#/employer/new">Đăng tin mới</a>')}</section>${pager(report.jobs,route,{})}`);
}

export async function employerCandidates(ctx, route) {
    const directory = route === "/employer/candidates";
    const keyword = ctx.query.get("keyword") || "";
    const status = !directory && Object.hasOwn(statuses,ctx.query.get("status")) ? ctx.query.get("status") : "";
    const result = await api(url(directory ? route : "/employer/applications", {keyword,status,page:pageOf(ctx.query),pageSize:10}), {signal:ctx.signal});
    if (redirectPastLastPage(result,route,{keyword,status})) return;
    const tabs = [["/employer/applicants","Đã ứng tuyển"],["/employer/candidates","Hồ sơ công khai"]];
    const list = result.items.map(item => directory ?
        `<article class="application-card"><div><h3><a href="#/employer/candidates/${item.id}">${esc(item.fullName)}</a></h3>
        <p>${esc(item.professionalTitle)}</p><p class="metadata">${esc(item.email)}</p></div>
        <a class="secondary small" href="#/employer/candidates/${item.id}">Xem hồ sơ</a></article>` :
        `<article class="application-card"><div><h3><a href="#/applications/${item.id}">${esc(item.candidateName)}</a></h3><p>${esc(item.jobTitle)}</p>
        <p class="metadata">${esc(item.candidateEmail)} · Nộp ngày ${date(item.submittedAt)}</p></div>
        <div class="actions">${badge(item.status)}<a class="secondary small" href="#/applications/${item.id}">Xem đơn</a></div></article>`).join("");
    ctx.show(`<header class="page-intro"><h1>Ứng viên</h1></header>
        <nav class="chips" aria-label="Danh sách ứng viên">${tabs.map(([path,label]) => `<a href="#${path}" class="chip ${path === route ? "current" : ""}" ${path === route ? 'aria-current="page"' : ""}>${label}</a>`).join("")}</nav>
        <form class="toolbar actions" id="candidate-search">
          <label class="field">Tìm kiếm<input type="search" name="keyword" maxlength="150" value="${esc(keyword)}" placeholder="${directory ? "Tên, email hoặc vị trí mong muốn" : "Tên, email hoặc vị trí đã nộp"}"></label>
          ${directory ? "" : `<label class="field">Trạng thái đơn<select name="status"><option value="">Tất cả trạng thái</option>${Object.entries(statuses).map(([key,label]) => `<option value="${key}" ${status === key ? "selected" : ""}>${label}</option>`).join("")}</select></label>`}
          <button class="primary" type="submit">Tìm kiếm</button></form>
        <p class="count">${result.totalCount} ${directory ? "hồ sơ công khai" : "đơn ứng tuyển"}</p>
        <div class="application-list">${list || empty(keyword || status ? "Không tìm thấy kết quả" : directory ? "Chưa có hồ sơ công khai" : "Chưa có đơn ứng tuyển",
            keyword || status ? "Thử đổi điều kiện tìm kiếm." : directory ? "Ứng viên xuất hiện tại đây khi họ bật công khai trong Hồ sơ và tài khoản." : "Các đơn nộp vào tin của công ty sẽ xuất hiện tại đây.")}</div>${pager(result,route,{keyword,status})}`);
    document.getElementById("candidate-search").onsubmit = event => {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        go(url(route,{keyword:form.get("keyword").trim(),status:form.get("status")}));
    };
}

export async function publicCandidatePage(ctx, id) {
    const profile = await api(`/employer/candidates/${id}`, {signal:ctx.signal});
    ctx.show(`<div class="narrow">${back("/employer/candidates","Hồ sơ công khai")}
        <article class="panel stack"><header><h1>${esc(profile.fullName)}</h1><p class="metadata">${esc(profile.professionalTitle)}</p></header>
          <div><h2>Email liên hệ</h2><p>${esc(profile.email)}</p></div>
          ${profile.profileSummary ? `<div><h2>Giới thiệu bản thân</h2><p class="body-copy">${esc(profile.profileSummary)}</p></div>` : ""}</article></div>`);
}
