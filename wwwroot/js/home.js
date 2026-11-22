import {api, user} from "./api.js";
import {card, searchForm} from "./jobs.js";
import {art} from "./layout.js";
import {avatar, date, empty, esc, go, url} from "./ui.js";

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
      <section class="wrap home-section" id="closing-jobs" hidden><div class="section-head"><h2>Vị trí sắp hết hạn</h2>
      <a class="secondary view-all" href="#/jobs?sort=Deadline">Xem tất cả ${art("arrow-blue")}</a></div><div class="vacancies"></div></section>
      <section class="process"><div class="wrap home-section"><h2>Ứng tuyển trên Việc Mở</h2><ol class="steps">
      ${steps.map(([symbol,title,copy]) => `<li><span>${art(symbol)}</span><h3>${title}</h3><p>${copy}</p></li>`).join("")}
      </ol></div></section>
      <section class="wrap home-section localities" id="localities" hidden><h2>Tìm việc theo địa điểm</h2>
        <div class="location-list"></div></section>
      <section class="wrap home-section"><div class="section-head"><h2>Việc làm mới nhất</h2>
        <a class="secondary view-all" href="#/jobs">Xem tất cả ${art("arrow-blue")}</a></div>
        <div id="recent-jobs" aria-live="polite"><p class="loading">Đang tải việc làm…</p></div></section>
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
    const [summary, recent, closing] = await Promise.allSettled([
        api("/jobs/summary",options), api("/jobs?pageSize=6",options), api("/jobs?sort=Deadline&pageSize=8",options)
    ]);
    ctx.signal.throwIfAborted();
    for (const [key] of stats) {
        document.querySelector(`[data-stat="${key}"]`).textContent = summary.status === "fulfilled"
            ? summary.value[key].toLocaleString("vi-VN") : "—";
    }
    const listings = document.querySelector("#recent-jobs");
    if (recent.status === "rejected") {
        listings.innerHTML = `<p class="error-panel">${esc(recent.reason.message)}</p><button class="secondary retry-home">Thử lại</button>`;
        listings.querySelector("button").onclick = () => go("/");
    } else {
        listings.innerHTML = recent.value.items.length ? `<div class="cards">${recent.value.items.map(job => card(job,false)).join("")}</div>` :
            empty("Chưa có tin đang tuyển","Các vị trí đang nhận hồ sơ sẽ xuất hiện tại đây.");
    }
    if (recent.status === "fulfilled" && recent.value.items.length) {
        const jobs = recent.value.items;
        const locations = [...new Set(jobs.map(job => job.location))];
        const cityBlock = document.querySelector("#localities");
        cityBlock.hidden = false;
        cityBlock.querySelector(".location-list").innerHTML = locations.map(city =>
            `<a href="#${esc(url("/jobs",{location:city}))}"><span>${art("map")}</span>${esc(city)}</a>`).join("");
        const companies = new Map(jobs.map(job => [job.employerId, job]));
        const employerBlock = document.querySelector("#companies");
        employerBlock.hidden = false;
        employerBlock.querySelector(".cards").innerHTML = [...companies.values()].map(job =>
            `<article class="company-box"><div class="card-company">${avatar(job.companyName)}<div><strong>${esc(job.companyName)}</strong>
            <span class="job-location">${art("pin")}${esc(job.location)}</span></div></div>
            <a href="#${esc(url("/jobs",{keyword:job.companyName}))}">Xem việc đang tuyển</a></article>`).join("");
    }
    if (closing.status === "fulfilled" && closing.value.items.length) {
        const block = document.querySelector("#closing-jobs");
        block.hidden = false;
        block.querySelector(".vacancies").innerHTML = closing.value.items.map(job =>
            `<div><a href="#/job/${job.id}">${esc(job.title)}</a><small>Hạn nộp ${date(job.deadline)}</small></div>`).join("");
    }
}
