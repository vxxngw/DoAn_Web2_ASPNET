const paths = {
    search: '<circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/>',
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
    return /^\/(?:admin\/(?:users|jobs(?:\/\d+\/edit)?)|jobs|job\/\d+|applications(?:\/\d+)?|employer(?:\/new|\/\d+\/(?:edit|applications))?)(?:\?.*)?$/.test(value || "") ? value : fallback;
}

export function date(value, withTime = false) {
    const options = withTime ? {dateStyle:"short", timeStyle:"short"} : {dateStyle:"short"};
    return new Intl.DateTimeFormat("vi-VN", options).format(new Date(value));
}

export function avatar(name) {
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
export function pager(result, path, params = {}) {
    if (!result || result.totalCount === 0) return "";
    const totalPages = result.totalPages ?? (result.pageSize > 0 ? Math.ceil(result.totalCount / result.pageSize) : 1);
    const currentPage = result.page || 1;
    const pageSize = result.pageSize || 10;
    const totalCount = result.totalCount;

    const start = (currentPage - 1) * pageSize + 1;
    const end = Math.min(currentPage * pageSize, totalCount);

    // Xây dựng danh sách số trang hiển thị
    const pageItems = [];
    if (totalPages <= 7) {
        for (let i = 1; i <= totalPages; i++) pageItems.push(i);
    } else {
        pageItems.push(1);
        if (currentPage > 3) pageItems.push("...");
        const startPage = Math.max(2, currentPage - 1);
        const endPage = Math.min(totalPages - 1, currentPage + 1);
        for (let i = startPage; i <= endPage; i++) pageItems.push(i);
        if (currentPage < totalPages - 2) pageItems.push("...");
        pageItems.push(totalPages);
    }

    const link = (p, label, title, className = "pager-item") =>
        `<a class="${className}" href="#${esc(url(path, {...params, page: p, pageSize}))}" title="${esc(title || label)}" aria-label="${esc(title || label)}">${label}</a>`;

    const disabledBtn = (label, title, className = "pager-nav") =>
        `<span class="${className} disabled" aria-disabled="true" title="${esc(title || label)}">${label}</span>`;

    const navButtons = [];
    if (totalPages > 1) {
        navButtons.push(currentPage > 1 ? link(1, "«", "Trang đầu", "pager-nav") : disabledBtn("«", "Trang đầu"));
        navButtons.push(currentPage > 1 ? link(currentPage - 1, "‹", "Trang trước", "pager-nav") : disabledBtn("‹", "Trang trước"));

        for (const item of pageItems) {
            if (item === "...") {
                navButtons.push('<span class="pager-ellipsis" aria-hidden="true">…</span>');
            } else if (item === currentPage) {
                navButtons.push(`<span class="pager-item current" aria-current="page">${item}</span>`);
            } else {
                navButtons.push(link(item, String(item), `Trang ${item}`));
            }
        }

        navButtons.push(currentPage < totalPages ? link(currentPage + 1, "›", "Trang sau", "pager-nav") : disabledBtn("›", "Trang sau"));
        navButtons.push(currentPage < totalPages ? link(totalPages, "»", "Trang cuối", "pager-nav") : disabledBtn("»", "Trang cuối"));
    }

    // Tùy chọn số mục trên mỗi trang
    const sizeOptions = [3, 5, 10, 15, 20];
    if (!sizeOptions.includes(pageSize)) sizeOptions.push(pageSize);
    sizeOptions.sort((a, b) => a - b);

    const sizeSelectOptions = sizeOptions.map(size => {
        const targetUrl = "#" + esc(url(path, {...params, page: 1, pageSize: size}));
        return `<option value="${targetUrl}" ${size === pageSize ? "selected" : ""}>${size} / trang</option>`;
    }).join("");

    const sizeSelector = `<label class="pager-size-label">Hiển thị:
      <select class="pager-size-select" onchange="location.hash=this.value" aria-label="Chọn số mục mỗi trang">
        ${sizeSelectOptions}
      </select>
    </label>`;

    const info = totalPages > 1
        ? `<span>Hiển thị <strong>${start}–${end}</strong> trong số <strong>${totalCount}</strong> mục (Trang ${currentPage}/${totalPages})</span>`
        : `<span>Hiển thị tất cả <strong>${totalCount}</strong> mục</span>`;

    return `<nav class="pager" aria-label="Phân trang">
      <div class="pager-meta">
        ${info}
        ${sizeSelector}
      </div>
      ${totalPages > 1 ? `<div class="pager-bar" role="navigation">${navButtons.join("")}</div>` : ""}
    </nav>`;
}

export function pageOf(query) {
    return Math.min(1000000, Math.max(1, parseInt(query.get("page") || query.get("pageNumber"), 10) || 1));
}

export function pageSizeOf(query, defaultSize = 10) {
    return Math.min(100, Math.max(1, parseInt(query.get("pageSize"), 10) || defaultSize));
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
