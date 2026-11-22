import {adminUsersPage} from "./admin.js";
import {dashboard} from "./layout.js";
import {api, clearSession, hasSession, setUser, user} from "./api.js";
import {accountPage} from "./account.js";
import {applicationsPage, applicationPage} from "./applications.js";
import {jobForm} from "./employer.js";
import {homePage} from "./home.js";
import {jobsPage, jobPage} from "./jobs.js";
import {esc, go, notify, url} from "./ui.js";

const main = document.querySelector("#main");
let current;
let previousPath = "";

function navigation(path) {
    const nav = document.querySelector("#nav");
    const account = document.querySelector("#account");
    const admin = user?.role === "Admin";
    const employer = user?.role === "Employer";
    const candidate = user?.role === "Candidate";
    const links = [["/", "Trang chủ"], ["/jobs", "Tìm việc"]];
    if (admin) links.push(["/admin/users","Quản trị"]);
    if (!admin && !employer) links.push([candidate ? "/applications" : "/register", candidate ? "Đơn đã nộp" : "Ứng viên"]);
    if (!admin && !candidate) links.push([employer ? "/employer" : "/register?role=Employer", employer ? "Tin của tôi" : "Nhà tuyển dụng"]);
    nav.innerHTML = links.map(([target,label]) => {
        const selected = target === path || (target === "/admin/users" && path.startsWith("/admin/")) || (target === "/jobs" && path.startsWith("/job/")) ||
            (target === "/employer" && path.startsWith("/employer/")) ||
            (target === "/applications" && path.startsWith("/applications/"));
        return `<a href="#${target}" ${selected ? 'aria-current="page"' : ""}>${label}</a>`;
    }).join("");
    const candidateLinks = document.querySelector("#footer-candidate");
    const employerLinks = document.querySelector("#footer-employer");
    candidateLinks.hidden = employer || admin;
    employerLinks.hidden = candidate || admin;
    candidateLinks.innerHTML = `<h2>Ứng viên</h2><a href="#/jobs">Tìm việc làm</a>
        <a href="#${candidate ? "/applications" : "/register"}">${candidate ? "Đơn đã nộp" : "Tạo tài khoản"}</a>`;
    employerLinks.innerHTML = `<h2>Nhà tuyển dụng</h2><a href="#${employer ? "/employer" : "/register?role=Employer"}">${employer ? "Tin của tôi" : "Tạo tài khoản công ty"}</a>
        <a href="#/employer/new">Đăng tin tuyển dụng</a>`;
    if (user) {
        account.innerHTML = `<span class="user-name">${esc(user.fullName)}</span><button id="logout" class="text-action">Đăng xuất</button>`;
        document.querySelector("#logout").onclick = () => {
            clearSession();
            go("/jobs");
            notify("Đã đăng xuất.");
        };
    } else {
        account.innerHTML = '<a class="secondary" href="#/login">Đăng nhập</a><a class="primary" href="#/register?role=Employer">Đăng tuyển</a>';
    }
}

function requireUser(role, destination) {
    if (!user) {
        location.replace("#" + url("/login", {next:destination}));
        return false;
    }
    if (role && user.role !== role) throw new Error("Vai trò của tài khoản không có quyền mở trang này.");
    return true;
}

async function render() {
    current?.abort();
    if (document.querySelector("#confirm-dialog").open) document.querySelector("#confirm-dialog").close("cancel");
    current = new AbortController();
    const signal = current.signal;
    const destination = location.hash.slice(1) || "/";
    const [path, rawQuery = ""] = destination.split("?");
    const auth = path === "/login" || path === "/register";
    const managed = path.startsWith("/admin/") || path.startsWith("/employer") || path.startsWith("/applications");
    document.body.classList.toggle("auth-page", auth);
    document.querySelector("#quick-search").elements.keyword.value = path === "/jobs" ? new URLSearchParams(rawQuery).get("keyword") || "" : "";
    const ctx = {signal, query:new URLSearchParams(rawQuery), show(markup) {
        signal.throwIfAborted();
        main.innerHTML = managed && user ? dashboard(markup, path) : markup;
    }};
    if (path !== previousPath) {
        window.scrollTo(0,0);
        main.focus({preventScroll:true});
    }
    previousPath = path;
    navigation(path === "/register" && ctx.query.get("role") === "Employer" ? "/register?role=Employer" : path);
    ctx.show('<p class="wrap loading">Đang tải…</p>');

    try {
        let match;
        if (path === "/") await homePage(ctx);
        else if (path === "/jobs") await jobsPage(ctx);
        else if ((match = path.match(/^\/job\/(\d+)$/))) await jobPage(ctx,match[1]);
        else if (path === "/login" || path === "/register") accountPage(ctx,path === "/register");
        else if (path === "/admin/users" && requireUser("Admin",destination)) await adminUsersPage(ctx);
        else if (path === "/admin/jobs" && requireUser("Admin",destination)) await jobsPage(ctx,false,true);
        else if ((match = path.match(/^\/admin\/jobs\/(\d+)\/edit$/)) && requireUser("Admin",destination)) await jobForm(ctx,match[1]);
        else if (path === "/employer" && requireUser("Employer",destination)) await jobsPage(ctx,true);
        else if (path === "/employer/new" && requireUser("Employer",destination)) await jobForm(ctx);
        else if ((match = path.match(/^\/employer\/(\d+)\/edit$/)) && requireUser("Employer",destination)) await jobForm(ctx,match[1]);
        else if ((match = path.match(/^\/employer\/(\d+)\/applications$/)) && requireUser("Employer",destination)) await applicationsPage(ctx,match[1]);
        else if (path === "/applications" && requireUser("Candidate",destination)) await applicationsPage(ctx);
        else if ((match = path.match(/^\/applications\/(\d+)$/)) && requireUser(null,destination)) await applicationPage(ctx,match[1]);
        else if (location.hash.slice(1) === destination) throw new Error("Không tìm thấy trang.");
        signal.throwIfAborted();
        document.title = (main.querySelector("h1")?.textContent || "Tìm việc") + " · Việc Mở";
    } catch (error) {
        if (signal.aborted) return;
        ctx.show(`<section class="wrap panel stack error-view"><h1>Chưa thể mở trang</h1><p class="error-panel">${esc(error.message)}</p>
          <div class="actions"><button class="secondary" id="retry">Thử lại</button><a class="primary" href="#/jobs">Tìm việc</a></div></section>`);
        document.querySelector("#retry").onclick = render;
    }
}

document.querySelector("#quick-search").onsubmit = event => {
    event.preventDefault();
    go(url("/jobs",{keyword:event.currentTarget.elements.keyword.value.trim()}));
};
window.addEventListener("session-expired", () => {
    const next = location.hash.slice(1) || "/";
    go(url("/login",{next}));
    notify("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
});
document.querySelector(".skip").onclick = event => {
    event.preventDefault();
    main.focus();
};
async function start() {
    if (hasSession()) {
        try { setUser(await api("/auth/me")); }
        catch (error) {
            if (error.status !== 401) {
                main.innerHTML = `<div class="panel stack"><p class="error-panel">${esc(error.message)}</p><button class="secondary" id="retry-start">Thử lại</button></div>`;
                document.querySelector("#retry-start").onclick = start;
                return;
            }
        }
    }
    window.addEventListener("hashchange",render);
    window.addEventListener("refresh-view",render);
    await render();
}
start();
