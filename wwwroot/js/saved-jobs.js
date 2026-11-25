import {api} from "./api.js";
import {date, empty, esc, go, icon, notify, pager, pageSizeOf, salary, url} from "./ui.js";

export async function savedJobsPage(ctx) {
    const page = Math.max(1, Number(ctx.query.get("page")) || 1);
    const pageSize = pageSizeOf(ctx.query, 5);
    const result = await api(`/jobs/saved?page=${page}&pageSize=${pageSize}`, {signal: ctx.signal});
    const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));

    const rows = result.items.map(item => `
        <article class="application-card">
            <div>
                <h3><a href="#/job/${item.jobId}">${esc(item.title)}</a></h3>
                <p class="metadata">
                    <strong>${esc(item.companyName)}</strong> · ${esc(item.location)}
                    ${item.minSalary != null || item.maxSalary != null ? ` · <span class="job-salary inline">${icon("tag")}${salary(item.minSalary, item.maxSalary)}</span>` : ""}
                </p>
                <p class="muted small">Lưu ngày: ${date(item.savedAt)} · Hạn nộp: ${date(item.deadline)}</p>
            </div>
            <div class="actions">
                <span class="badge ${item.isOpen ? "open" : "closed"}">${item.isOpen ? "Đang mở" : "Đã đóng"}</span>
                <a class="primary small" href="#/job/${item.jobId}">Ứng tuyển</a>
                <button type="button" class="danger small" data-unsave="${item.jobId}">Bỏ lưu</button>
            </div>
        </article>`).join("");

    const noResults = empty(
        "Chưa có việc làm đã lưu",
        "Hãy khám phá các cơ hội nghề nghiệp và bấm Lưu tin để xem lại khi cần.",
        '<a class="primary" href="#/jobs">Khám phá việc làm</a>');

    const pageSubtitle = totalPages > 1 ? ` · Trang ${result.page}/${totalPages}` : "";

    ctx.show(`
        <header class="page-intro">
            <h1>Việc làm đã lưu</h1>
            <p>Danh sách các công việc bạn đã quan tâm và lưu lại để ứng tuyển.</p>
        </header>
        <div class="toolbar">
            <span class="count">${result.totalCount} công việc đã lưu${pageSubtitle}</span>
        </div>
        <div class="application-list">${rows || noResults}</div>
        ${pager(result, "/saved-jobs", {pageSize})}`);

    const list = document.querySelector(".application-list");
    if (list) {
        list.onclick = async event => {
            const btn = event.target.closest("button[data-unsave]");
            if (!btn) return;
            const jobId = Number(btn.dataset.unsave);
            btn.disabled = true;
            try {
                await api(`/jobs/${jobId}/save`, {method: "DELETE"});
                notify("Đã bỏ lưu tin tuyển dụng.");
                go(url("/saved-jobs", {page, pageSize}));
            } catch (err) {
                notify(err.message);
                btn.disabled = false;
            }
        };
    }
}
