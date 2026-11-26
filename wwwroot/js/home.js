import {api, user} from "./api.js";
import {cities, salaryText, searchForm} from "./jobs.js";
import {saveButton} from "./saved.js";
import {art} from "./layout.js";
import {companyAvatar, date, esc, go, icon, url} from "./ui.js";

const suggestions = ["Thiết kế", "Lập trình", "Marketing", "Thực tập"];
const stats = [
    ["openJobs","briefcase","Việc đang tuyển"], ["companies","buildings","Nhà tuyển dụng"],
    ["locations","map","Địa điểm làm việc"], ["newThisWeek","briefcase","Tin mới trong 7 ngày"]
];
const steps = [
    ["users","Tạo tài khoản","Chọn tài khoản ứng viên và đăng ký bằng email."],
    ["search","Tìm việc phù hợp","Tìm theo vị trí, công ty hoặc nơi làm việc."],
    ["briefcase","Gửi hồ sơ","Đính kèm CV ở định dạng PDF và gửi lời nhắn."],
    ["calendar","Theo dõi kết quả","Xem phản hồi của nhà tuyển dụng trong Đơn đã nộp."]
];

function companyTile(company) {
    const words = company.companyName.trim().split(/\s+/);
    const initials = words.slice(-2).map(word => word[0]).join("").toLocaleUpperCase("vi");
    return `<a class="company-tile" href="#${esc(url("/jobs",{employerId:company.employerId}))}">
      ${company.hasCompanyLogo ? `<img class="company-logo" src="/api/companies/${company.employerId}/logo" alt="">` :
        `<span class="company-mark tone-${company.employerId % 6}" aria-hidden="true">${esc(initials)}</span>`}
      <strong>${esc(company.companyName)}</strong>
      <span class="opening-count">${company.openJobs} việc đang tuyển ${icon("right")}</span></a>`;
}

function featuredJob(job) {
    return `<article class="featured-job">${companyAvatar(job)}
      <div><h3><a href="#/job/${job.id}">${esc(job.title)}</a></h3><p>${esc(job.companyName)}</p>
        <span class="job-salary">Lương · ${salaryText(job)}</span>
        <span class="featured-facts"><span>${icon("pin")}${esc(job.location)}</span>
        <span>${icon("calendar")}Hạn nộp ${date(job.deadline)}</span></span></div>${saveButton(job) || icon("right")}</article>`;
}

function bindFeaturedLocations(ctx) {
    const filters = document.getElementById("featured-locations");
    const block = document.getElementById("featured-jobs");
    const all = document.getElementById("all-featured");
    let requestNumber = 0;
    async function load(city) {
        const number = ++requestNumber;
        for (const button of filters.querySelectorAll("button")) {
            const current = button.dataset.city === city;
            button.classList.toggle("current",current);
            button.setAttribute("aria-pressed",String(current));
        }
        const params = {sort:"Featured",location:city};
        all.href = "#" + url("/jobs",params);
        block.innerHTML = '<p class="loading">Đang tải việc làm…</p>';
        try {
            const page = await api(url("/jobs",{...params,pageSize:6}),{signal:ctx.signal});
            if (ctx.signal.aborted || number !== requestNumber) return;
            block.innerHTML = page.items.length ? `<div class="featured-grid">${page.items.map(featuredJob).join("")}</div>` :
                '<p class="home-empty">Chưa có việc làm đang tuyển tại địa điểm này.</p>';
        } catch (error) {
            if (ctx.signal.aborted || number !== requestNumber) return;
            block.innerHTML = `<p class="error-panel">${esc(error.message)}</p><button class="secondary">Thử lại</button>`;
            block.querySelector("button").onclick = () => load(city);
        }
    }
    filters.onclick = event => {
        const button = event.target.closest("button");
        if (button) load(button.dataset.city);
    };
    return load("");
}

function showHomeResults(id, result, render, emptyMessage) {
    const block = document.getElementById(id);
    if (result.status === "rejected") {
        block.innerHTML = `<p class="error-panel">${esc(result.reason.message)}</p><button class="secondary retry-home">Thử lại</button>`;
        block.querySelector("button").onclick = () => go("/");
        return;
    }
    block.innerHTML = render(result.value) || `<p class="home-empty">${emptyMessage}</p>`;
}

export async function homePage(ctx) {
    const quickLinks = suggestions.map(label =>
        `<a href="#${esc(url("/jobs",{keyword:label}))}">${esc(label)}</a>`).join(", ");
    ctx.show(`<section class="hero"><div class="wrap"><div class="hero-grid">
      <div class="hero-copy"><h1>Tìm công việc phù hợp với sở thích và kỹ năng.</h1>
      <p>Khám phá các vị trí đang tuyển, kết nối với nhà tuyển dụng và ứng tuyển bằng CV của bạn.</p>
      ${searchForm({})}<div class="suggestions"><span>Gợi ý</span> ${quickLinks}</div></div>
      <img class="hero-art" src="/assets/jobpilot/illustration.svg" width="492" height="382" alt="" fetchpriority="high">
      </div><dl class="stats" id="home-stats">${stats.map(([key,symbol,label]) =>
        `<div class="stat"><span class="stat-icon">${art(symbol)}</span><div><dt>${label}</dt><dd data-stat="${key}">…</dd></div></div>`).join("")}</dl>
      </div></section>
      <section class="top-companies" aria-labelledby="top-companies-title"><div class="wrap">
        <h2 id="top-companies-title">Các công ty hàng đầu</h2>
        <div id="top-companies" aria-live="polite"><p>Đang tải công ty…</p></div></div></section>
      <section class="wrap home-section featured-section" aria-labelledby="featured-title"><div class="section-head">
        <h2 id="featured-title">Việc làm nổi bật</h2><a class="secondary view-all" id="all-featured" href="#/jobs?sort=Featured">Xem tất cả ${art("arrow-blue")}</a></div>
        <div class="chips" role="group" aria-label="Lọc việc nổi bật theo địa điểm" id="featured-locations">
        ${["",...cities].map(city => `<button type="button" class="chip" data-city="${esc(city)}">${esc(city || "Tất cả")}</button>`).join("")}</div>
        <div id="featured-jobs" aria-live="polite"><p class="loading">Đang tải việc làm…</p></div></section>
      <section class="wrap home-section" aria-labelledby="industries-title"><div class="section-head"><h2 id="industries-title">Ngành nghề trọng điểm</h2></div>
        <div id="home-industries" aria-live="polite"><p class="loading">Đang tải ngành nghề…</p></div></section>
      <section class="process"><div class="wrap home-section"><h2>Ứng tuyển trên Việc Mở</h2><ol class="steps">
      ${steps.map(([symbol,title,copy]) => `<li><span>${art(symbol)}</span><h3>${title}</h3><p>${copy}</p></li>`).join("")}
      </ol></div></section>
      <section class="wrap home-section companies" id="companies" hidden><h2>Công ty đang tuyển</h2><div class="cards"></div></section>
      ${!user ? `<section class="wrap role-ctas">
      <article><h2>Dành cho ứng viên</h2><p>Tạo tài khoản để gửi CV và theo dõi các đơn đã nộp.</p><a class="secondary" href="#/register">Đăng ký ứng viên ${art("arrow-blue")}</a></article>
      <article><h2>Dành cho nhà tuyển dụng</h2><p>Đăng tin, nhận CV và xét hồ sơ ứng viên tại một nơi.</p><a class="secondary" href="#/register?role=Employer">Đăng ký tuyển dụng ${art("arrow-blue")}</a></article></section>` : ""}`);
    document.querySelector("#search-jobs").onsubmit = event => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        go(url("/jobs",{keyword:data.get("keyword").trim(),location:data.get("location").trim()}));
    };
    const options = {signal:ctx.signal};
    const [summary, recent, companies, industries] = await Promise.allSettled([
        api("/jobs/summary",options), api("/jobs?pageSize=6",options),
        api("/jobs/top-companies",options), api("/jobs/industries",options), bindFeaturedLocations(ctx)
    ]);
    ctx.signal.throwIfAborted();
    for (const [key] of stats) {
        document.querySelector(`[data-stat="${key}"]`).textContent = summary.status === "fulfilled"
            ? summary.value[key].toLocaleString("vi-VN") : "—";
    }
    showHomeResults("top-companies", companies, rows => rows.length ? `<div class="company-tiles">${rows.map(companyTile).join("")}</div>` : "",
        "Chưa có công ty đang tuyển.");
    showHomeResults("home-industries", industries, rows => rows.length ? `<div class="industry-grid">${rows.slice(0,6).map(row =>
        `<a class="industry-tile" href="#${esc(url("/jobs",{industry:row.industry}))}"><span class="industry-symbol">${icon("work")}</span>
        <span><strong>${esc(row.industry)}</strong><small>${row.openJobs} việc đang tuyển</small></span>${icon("right")}</a>`).join("")}</div>` : "",
        "Chưa có ngành nghề có tin đang tuyển.");
    if (recent.status === "fulfilled" && recent.value.items.length) {
        const jobs = recent.value.items;
        const companies = new Map(jobs.map(job => [job.employerId, job]));
        const employerBlock = document.querySelector("#companies");
        employerBlock.hidden = false;
        employerBlock.querySelector(".cards").innerHTML = [...companies.values()].map(job =>
            `<article class="company-box"><div class="card-company">${companyAvatar(job)}<div><strong>${esc(job.companyName)}</strong>
            <span class="job-location">${art("pin")}${esc(job.location)}</span></div></div>
            <a href="#${esc(url("/jobs",{keyword:job.companyName}))}">Xem việc đang tuyển</a></article>`).join("");
    }
}
