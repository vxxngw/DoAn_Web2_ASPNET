const paths = {
    bookmark: '<path d="M6 4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v17l-6-4-6 4z"/>',
    overview: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
    person: '<circle cx="12" cy="8" r="3.4"/><path d="M5 20v-2a7 7 0 0 1 14 0v2"/>',
    account: '<rect x="3" y="5" width="18" height="14" rx="2"/><circle cx="9" cy="11" r="2"/><path d="M6 16a3 3 0 0 1 6 0m2-6h4m-4 4h3"/>',
    logout: '<path d="M10 4H5v16h5m-1-8h12m-5-5 5 5-5 5"/>',
    search: '<circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/>',
    eye: '<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>',
    eyeOff: '<path d="m3 3 18 18M10.6 5.1 12 5c6.5 0 10 7 10 7a20 20 0 0 1-3 3.9M6.4 6.4A19 19 0 0 0 2 12s3.5 7 10 7c1.9 0 3.6-.6 5.1-1.5M10 10a2.8 2.8 0 0 0 4 4"/>',
    pin: '<path d="M19 10c0 5-7 11-7 11S5 15 5 10a7 7 0 1 1 14 0Z"/><circle cx="12" cy="10" r="2"/>',
    calendar: '<rect x="3" y="5" width="18" height="16" rx="3"/><path d="M7 3v4m10-4v4M3 10h18"/>',
    work: '<rect x="3" y="7" width="18" height="14" rx="3"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12c5 3 13 3 18 0m-9 1v3"/>',
    file: '<path d="M6 3h8l5 5v13H6zM14 3v6h5M9 13h7m-7 4h5"/>',
    clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    plus: '<path d="M12 4v16M4 12h16"/>',
    down: '<path d="m7 10 5 5 5-5"/>',
    left: '<path d="m14 7-5 5 5 5"/>',
    right: '<path d="m10 7 5 5-5 5"/>',
    check: '<path d="m4 12 5 5L20 6"/>',
    sort: '<path d="M3 6h17M3 12h12M3 18h7"/>'
};
export function icon(name) {
    return `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name]}</svg>`;
}

export function esc(value) {
    return String(value ?? "").replace(/[&<>"']/g, c => ({"&":"&amp;", "<":"&lt;", ">":"&gt;", '"':"&quot;", "'":"&#39;"})[c]);
}

export function url(path, values = {}) {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(values)) {
        if (value !== undefined && value !== null && value !== "") params.set(key, value);
    }
    return path + (params.size ? "?" + params.toString() : "");
}

export function go(path) {
    if (location.hash === "#" + path) window.dispatchEvent(new Event("refresh-view"));
    else location.hash = path;
}

export function internalNext(value, fallback) {
    return /^\/(?:profile|account\/settings|admin\/(?:(?:users|jobs)(?:\/\d+\/edit)?|statistics)|jobs|saved-jobs|job\/\d+|applications(?:\/\d+)?|employer(?:\/(?:new|report|applicants)|\/candidates(?:\/\d+)?|\/\d+\/(?:edit|applications))?)(?:\?.*)?$/.test(value || "") ? value : fallback;
}

export function date(value, withTime = false) {
    const options = withTime ? {dateStyle:"short", timeStyle:"short"} : {dateStyle:"short"};
    return new Intl.DateTimeFormat("vi-VN", options).format(new Date(value));
}

export function companyAvatar(company) {
    if (company.hasCompanyLogo)
        return `<img class="company-avatar company-avatar-image" src="/api/companies/${company.employerId}/logo" alt="" loading="lazy">`;
    const name = company.companyName;
    const words = name.trim().split(/\s+/);
    const letters = words.slice(0, 2).map(w => w[0]).join("").toUpperCase();
    const hue = [...name].reduce((sum, ch) => sum + ch.codePointAt(0), 0) % 6;
    return `<span class="company-avatar tone-${hue}" aria-hidden="true">${esc(letters)}</span>`;
}

export const statuses = {Submitted:"Mới nộp", Accepted:"Chấp nhận", Rejected:"Từ chối"};
export function badge(status) {
    return `<span class="badge ${esc(status)}">${esc(statuses[status] || status)}</span>`;
}
export function activeJob(job) {
    return !job.employerLocked && job.isOpen && new Date(job.deadline).getTime() > Date.now();
}

export function empty(title, message, action = "") {
    return `<div class="empty"><span class="empty-icon">${icon("work")}</span><h3>${esc(title)}</h3><p>${esc(message)}</p>${action}</div>`;
}
export function back(path, label) {
    return `<a class="back" href="#${esc(path)}">${icon("left")}${esc(label)}</a>`;
}
export function saveBlob(blob, fileName) {
    const address = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = address;
    link.download = fileName;
    document.body.append(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(address), 30000);
}
export function pager(result, path, params, pageKey = "page", label = "Phân trang") {
    const pages = Math.ceil(result.totalCount / result.pageSize);
    if (pages < 2) return "";
    const link = (page, text) => `<a class="secondary small" href="#${esc(url(path, {...params, [pageKey]:page}))}">${text}</a>`;
    return `<nav class="pager" aria-label="${esc(label)}">${result.page > 1 ? link(result.page-1,"Trang trước") : ""}<span>Trang ${result.page} / ${pages}</span>${result.page < pages ? link(result.page+1,"Trang sau") : ""}</nav>`;
}
export function pageOf(query, key = "page") {
    return Math.min(1000000, Math.max(1, parseInt(query.get(key), 10) || 1));
}

let timer;
export function notify(message) {
    const notice = document.querySelector("#notice");
    notice.textContent = message;
    clearTimeout(timer);
    timer = setTimeout(() => notice.textContent = "", 5000);
}
export function confirmAction(title, message, label) {
    const dialog = document.querySelector("#confirm-dialog");
    dialog.querySelector("#confirm-title").textContent = title;
    dialog.querySelector("#confirm-message").textContent = message;
    dialog.querySelector("#confirm-action").textContent = label;
    dialog.returnValue = "cancel";
    dialog.showModal();
    return new Promise(resolve => dialog.addEventListener("close", () => resolve(dialog.returnValue === "ok"), {once:true}));
}

export function bindForm(id, submit) {
    const form = document.getElementById(id);
    form.addEventListener("submit", async event => {
        event.preventDefault();
        const button = form.querySelector('button[type="submit"]');
        if (button.disabled) return;
        const errorBox = form.querySelector(".form-error");
        errorBox.textContent = "";
        button.disabled = true;
        try {
            await submit(new FormData(form), form);
        } catch (error) {
            errorBox.textContent = error.message;
            errorBox.focus();
        } finally {
            button.disabled = false;
        }
    });
}
export const errorBox = '<p class="form-error" role="alert" tabindex="-1"></p>';
