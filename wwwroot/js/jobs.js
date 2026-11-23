import {art, breadcrumb} from "./layout.js";
import {api, user} from "./api.js";
import {activeJob, avatar, bindForm, date, empty, errorBox, esc, go, icon, notify, pageOf, pageSizeOf, pager, salary, url} from "./ui.js";

const cities = ["TP. Hồ Chí Minh", "Hà Nội", "Đà Nẵng"];
const sorts = {Newest:"Mới nhất", Deadline:"Sắp hết hạn", Title:"Tên vị trí A–Z", SalaryDesc:"Lương cao nhất"};
const salaryRanges = [
    { label: "Mọi mức lương", min: null, max: null },
    { label: "Dưới 10 triệu", min: null, max: 10000000 },
    { label: "10 – 20 triệu", min: 10000000, max: 20000000 },
    { label: "20 – 30 triệu", min: 20000000, max: 30000000 },
    { label: "Trên 30 triệu", min: 30000000, max: null }
];

export function searchForm(params, filters = false) {
    return `<form id="search-jobs" class="search-form" role="search">
      <label class="search-field">${art("search")}<span class="sr-only">Từ khóa</span>
        <input name="keyword" type="search" maxlength="150" placeholder="Vị trí hoặc tên công ty" value="${esc(params.keyword)}"></label>
      <label class="search-field">${icon("pin")}<span class="sr-only">Địa điểm</span>
        <input name="location" maxlength="150" placeholder="Địa điểm làm việc" value="${esc(params.location)}"></label>
      ${filters ? `<button type="button" class="filter-button" id="toggle-filters" aria-expanded="false" aria-controls="job-filters">${icon("sort")}Bộ lọc</button>` : ""}
      <button class="primary" type="submit">Tìm việc</button></form>`;
}

export function card(job, mine) {
    const target = `/job/${job.id}`;
    return `<article class="job-card">
      <div class="job-card-header">
        <h3><a href="#${target}">${esc(job.title)}</a></h3>
        <span class="job-salary">${icon("tag")}${salary(job.minSalary, job.maxSalary)}</span>
      </div>
      <div class="job-facts">${jobBadge(job)}<span>Hạn nộp ${date(job.deadline)}</span></div>
      <div class="card-company">${avatar(job.companyName)}<div><strong>${esc(job.companyName)}</strong>
        <span class="job-location">${art("pin")}${esc(job.location)}</span></div>
        ${mine ? `<a class="card-link" href="#/employer/${job.id}/applications">Xem hồ sơ</a>` : ""}</div>
    </article>`;
}

function jobBadge(job) {
    const open = activeJob(job);
    return `<span class="badge ${open ? "open" : "closed"}">${open ? "Đang nhận đơn" : job.employerLocked ? "Công ty bị khóa" : job.isOpen ? "Hết hạn" : "Đã đóng"}</span>`;
}

export async function jobsPage(ctx, mine = false, allJobs = false) {
    const managed = mine || allJobs;
    const route = allJobs ? "/admin/jobs" : mine ? "/employer" : "/jobs";
    const page = pageOf(ctx.query);
    const pageSize = pageSizeOf(ctx.query, managed ? 10 : 5);
    const minSalary = ctx.query.get("minSalary") ? Number(ctx.query.get("minSalary")) : null;
    const maxSalary = ctx.query.get("maxSalary") ? Number(ctx.query.get("maxSalary")) : null;
    const params = {
        keyword: ctx.query.get("keyword") || "",
        location: ctx.query.get("location") || "",
        sort: Object.hasOwn(sorts, ctx.query.get("sort")) ? ctx.query.get("sort") : "Newest",
        minSalary: minSalary ?? "",
        maxSalary: maxSalary ?? "",
        pageSize
    };
    const search = searchForm(params, !managed);
    const intro = allJobs ? `<header class="page-intro"><h1>Quản lý tin tuyển dụng</h1></header>${search}` : mine
        ? `<div class="section-head page-intro"><div><h1>Tin của tôi</h1><p>${esc(user.companyName)}</p></div>
          <a class="primary" href="#/employer/new">${icon("plus")}Đăng tin mới</a></div>${search}`
        : breadcrumb("Tìm việc", null, true) + `<div class="wrap list-search">${search}</div>`;
    ctx.show(intro + `<section class="${managed ? "" : "wrap "}list-section" id="jobs-results" aria-live="polite"><p class="loading">Đang tải danh sách…</p></section>`);
    const toggle = document.querySelector("#toggle-filters");
    if (toggle) toggle.disabled = true;
    document.querySelector("#search-jobs").onsubmit = event => {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        go(url(route, {...params, page: 1, keyword:form.get("keyword").trim(), location:form.get("location").trim()}));
    };

    const result = await api(url(allJobs ? "/admin/jobs" : mine ? "/jobs/mine" : "/jobs", {...params, page, pageSize}), {signal:ctx.signal});
    ctx.signal.throwIfAborted();
    const totalPages = result.totalPages ?? Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    if (page > totalPages) { location.replace("#" + url(route,{...params,page:totalPages})); return; }

    const choices = ["", ...cities];
    if (params.location && !choices.includes(params.location)) choices.push(params.location);
    const chips = choices.map(city => {
        const selected = city === params.location;
        return `<a class="chip ${selected ? "current" : ""}" ${selected ? 'aria-current="true"' : ""}
          href="#${esc(url(route,{...params, location:city, page: 1}))}">${esc(city || "Mọi địa điểm")}</a>`;
    }).join("");

    const salaryChips = salaryRanges.map(range => {
        const isCurrent = (range.min === null ? !params.minSalary : params.minSalary == range.min) &&
                          (range.max === null ? !params.maxSalary : params.maxSalary == range.max);
        return `<a class="chip ${isCurrent ? "current" : ""}" ${isCurrent ? 'aria-current="true"' : ""}
          href="#${esc(url(route, {...params, minSalary: range.min, maxSalary: range.max, page: 1}))}">${esc(range.label)}</a>`;
    }).join("");

    const sort = Object.entries(sorts).map(([key, label]) =>
        `<option value="${key}" ${params.sort === key ? "selected" : ""}>${label}</option>`).join("");
    const filtered = Boolean(params.keyword || params.location || params.minSalary || params.maxSalary);
    const none = filtered
        ? empty("Chưa tìm thấy tin phù hợp", "Thử từ khóa khác, mở rộng địa điểm hoặc mức lương tìm kiếm.", `<a class="secondary" href="#${route}">Xóa bộ lọc</a>`)
        : empty(mine ? "Bạn chưa đăng tin nào" : "Chưa có tin đang tuyển", mine ? "Đăng tin đầu tiên để bắt đầu nhận hồ sơ." : "Tin sẽ hiển thị tại đây khi nhà tuyển dụng đăng và mở nhận đơn.",
            mine ? '<a class="primary" href="#/employer/new">Đăng tin đầu tiên</a>' : "");
    const pageSubtitle = totalPages > 1 ? ` · Trang ${result.page}/${totalPages}` : "";
    document.querySelector("#jobs-results").innerHTML = `<div id="job-filters" ${!managed && !filtered && params.sort === "Newest" ? "hidden" : ""}><div class="section-head"><div class="title-count">
      <h2>${managed ? "Danh sách tin" : "Việc làm đang tuyển"}</h2><span class="count">${result.totalCount} vị trí${pageSubtitle}</span></div>
      <label class="sort-control">${icon("sort")}Sắp xếp<select id="sort">${sort}</select></label></div>
      <div class="filters-group">
        <div class="chips" aria-label="Lọc địa điểm">${chips}</div>
        <div class="chips salary-chips" aria-label="Lọc mức lương">${salaryChips}</div>
      </div></div>
      ${result.items.length ? `<div class="cards">${result.items.map(j => card(j,mine)).join("")}</div>` : none}
      ${pager(result,route,params)}`;
    if (toggle) {
        const filters = document.querySelector("#job-filters");
        toggle.disabled = false;
        toggle.setAttribute("aria-expanded", String(!filters.hidden));
        toggle.onclick = () => {
            filters.hidden = !filters.hidden;
            toggle.setAttribute("aria-expanded", String(!filters.hidden));
        };
    }
    document.querySelector("#sort").onchange = e => go(url(route,{...params, sort:e.target.value, page: 1}));
}

export async function jobPage(ctx, id) {
    const [job, previous] = await Promise.all([
        api(`/jobs/${id}`,{signal:ctx.signal}),
        user?.role === "Candidate" ? api(`/applications/mine?jobId=${id}&pageSize=1`,{signal:ctx.signal}) : null
    ]);
    const owner = user?.id === job.employerId;
    const admin = user?.role === "Admin";
    let side;
    if (admin) {
        side = `<h2>Quản lý tin</h2><a class="primary" href="#/admin/jobs/${id}/edit">Chỉnh sửa tin</a>`;
    } else if (owner) {
        side = `<h2>Quản lý tin</h2>${jobBadge(job)}<div class="actions"><a class="primary" href="#/employer/${id}/applications">Hồ sơ ứng tuyển</a>
          <a class="secondary" href="#/employer/${id}/edit">Chỉnh sửa tin</a></div>`;
    } else if (previous?.items.length) {
        const app = previous.items[0];
        side = `<h2>Bạn đã ứng tuyển</h2><p class="muted">Đã nộp ngày ${date(app.submittedAt)}.</p><a class="primary" href="#/applications/${app.id}">Xem đơn của tôi</a>`;
    } else if (!user) {
        side = `<h2>Ứng tuyển vị trí này</h2><p class="muted">Đăng nhập bằng tài khoản ứng viên để gửi CV và lời nhắn.</p>
          <a class="primary" href="#${esc(url("/login",{next:`/job/${id}`}))}">Đăng nhập để ứng tuyển</a>`;
    } else if (user.role === "Candidate" && activeJob(job)) {
        side = `<h2>Ứng tuyển vị trí này</h2><form id="apply" class="stack">
          <label class="field">CV của bạn<input type="file" name="cv" accept=".pdf,application/pdf" required><span class="hint">Tệp PDF, tối đa 5 MiB.</span></label>
          <label class="field">Lời nhắn<textarea name="message" required maxlength="1500"></textarea></label>
          ${errorBox}<button class="primary" type="submit">Gửi hồ sơ</button></form>`;
    } else {
        side = `<h2>Thông tin tuyển dụng</h2>${jobBadge(job)}<p class="muted">Nhà tuyển dụng nhận hồ sơ từ tài khoản ứng viên.</p>`;
    }
    const parent = admin ? ["/admin/jobs","Quản lý tin tuyển dụng"] : owner ? ["/employer","Tin của tôi"] : ["/jobs","Tìm việc"];
    ctx.show(breadcrumb("Chi tiết việc làm", parent) + `<div class="wrap detail">
      <div class="detail-heading">${avatar(job.companyName)}<div><h1>${esc(job.title)}</h1>
      <div class="detail-company">${esc(job.companyName)} ${jobBadge(job)}</div></div>
      <button class="primary jump-apply" type="button" id="jump-apply">${admin || owner ? "Quản lý tin" : previous?.items.length ? "Xem đơn" : user?.role === "Employer" ? "Thông tin tuyển dụng" : "Ứng tuyển"}${art("arrow")}</button></div>
      <div class="detail-grid"><article class="description"><h2>Mô tả công việc</h2><p class="body-copy">${esc(job.description)}</p></article>
      <aside class="stack"><section class="panel overview"><h2>Thông tin công việc</h2><dl>
      <div>${art("calendar")}<dt>Ngày đăng</dt><dd>${date(job.createdAt)}</dd></div>
      <div>${art("timer")}<dt>Hạn nộp</dt><dd>${date(job.deadline,true)}</dd></div>
      <div>${art("map")}<dt>Địa điểm</dt><dd>${esc(job.location)}</dd></div>
      <div>${icon("tag")}<dt>Mức lương</dt><dd class="salary-highlight"><strong>${salary(job.minSalary, job.maxSalary)}</strong></dd></div></dl></section>
      <section class="panel stack apply-panel" id="apply-panel" tabindex="-1">${side}</section></aside></div></div><section class="wrap home-section related" hidden><h2>Việc làm cùng địa điểm</h2><div class="cards"></div></section>`);
    document.querySelector("#jump-apply").onclick = () => {
        const panel = document.querySelector("#apply-panel");
        const action = panel.querySelector("a.primary");
        if (action) action.click();
        else (panel.querySelector('input[type="file"]') || panel).focus();
    };
    if (document.querySelector("#apply")) bindForm("apply",async data => {
        if (data.get("cv").size > 5 * 1024 * 1024) throw new Error("CV vượt quá 5 MiB.");
        const created = await api(`/jobs/${id}/applications`,{method:"POST",body:data});
        if (ctx.signal.aborted) return;
        notify("Đã gửi hồ sơ ứng tuyển.");
        go(`/applications/${created.id}`);
    });
    const related = await api(url("/jobs",{location:job.location,pageSize:4}),{signal:ctx.signal}).catch(() => null);
    if (ctx.signal.aborted || !related) return;
    const others = related.items.filter(item => item.id !== job.id).slice(0,3);
    if (others.length) {
        const section = document.querySelector(".related");
        section.hidden = false;
        section.querySelector(".cards").innerHTML = others.map(item => card(item,false)).join("");
    }

}
