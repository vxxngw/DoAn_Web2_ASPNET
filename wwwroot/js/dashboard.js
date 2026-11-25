import {api} from "./api.js";
import {esc, icon, notify} from "./ui.js";

export async function adminDashboardPage(ctx) {
    const stats = await api("/dashboard/admin", {signal: ctx.signal});

    const maxLocationCount = Math.max(1, ...stats.topLocations.map(l => l.jobCount));
    const locationBars = stats.topLocations.map(loc => {
        const percent = Math.round((loc.jobCount / maxLocationCount) * 100);
        return `
            <div class="stat-bar-item">
                <div class="stat-bar-header">
                    <strong>${esc(loc.location)}</strong>
                    <span class="count">${loc.jobCount} tin</span>
                </div>
                <div class="stat-bar-track">
                    <div class="stat-bar-fill tone-accent" style="width: ${percent}%;"></div>
                </div>
            </div>`;
    }).join("");

    const statusMap = {
        Submitted: {label: "Chờ xét duyệt", color: "tone-warning"},
        Accepted: {label: "Đã duyệt nhận", color: "tone-success"},
        Rejected: {label: "Từ chối", color: "tone-danger"},
        Withdrawn: {label: "Đã rút hồ sơ", color: "tone-muted"}
    };

    const statusCards = stats.applicationStatusBreakdown.map(s => {
        const info = statusMap[s.status] || {label: s.status, color: "tone-accent"};
        return `
            <div class="kpi-mini-card ${info.color}">
                <span class="kpi-label">${info.label}</span>
                <strong class="kpi-number">${s.count}</strong>
            </div>`;
    }).join("");

    ctx.show(`
        <header class="page-intro">
            <h1>Thống kê tổng quan hệ thống</h1>
            <p>Báo cáo tổng hợp số liệu người dùng, tin tuyển dụng và hồ sơ ứng tuyển trên toàn nền tảng.</p>
        </header>

        <section class="kpi-grid">
            <div class="kpi-card">
                <span class="kpi-icon">${icon("file")}</span>
                <div>
                    <span class="kpi-label">Ứng viên đăng ký</span>
                    <strong class="kpi-value">${stats.totalCandidates}</strong>
                </div>
            </div>
            <div class="kpi-card">
                <span class="kpi-icon">${icon("work")}</span>
                <div>
                    <span class="kpi-label">Nhà tuyển dụng</span>
                    <strong class="kpi-value">${stats.totalEmployers}</strong>
                </div>
            </div>
            <div class="kpi-card">
                <span class="kpi-icon">${icon("check")}</span>
                <div>
                    <span class="kpi-label">Tin tuyển dụng</span>
                    <strong class="kpi-value">${stats.openJobs} <span class="muted small">/ ${stats.totalJobs} tin</span></strong>
                </div>
            </div>
            <a class="kpi-card dashboard-link" href="#/admin/applications?status=Accepted" style="text-decoration:none;">
                <span class="kpi-icon">${icon("tag")}</span>
                <div>
                    <span class="kpi-label">Tổng hồ sơ ứng tuyển</span>
                    <strong class="kpi-value">${stats.totalApplications} <span class="muted small">(${stats.acceptedApplications} đã duyệt)</span></strong>
                </div>
            </a>
        </section>

        <div class="dashboard-charts-grid">
            <section class="panel">
                <h2>${icon("pin")} Việc làm theo địa điểm tuyển dụng</h2>
                <div class="stat-bars-container">
                    ${locationBars || '<p class="muted">Chưa có dữ liệu địa điểm.</p>'}
                </div>
            </section>

            <section class="panel">
                <h2>${icon("file")} Phân bố trạng thái hồ sơ</h2>
                <div class="status-grid">
                    ${statusCards || '<p class="muted">Chưa có hồ sơ ứng tuyển nào.</p>'}
                </div>
            </section>
        </div>`);
}

export async function employerDashboardPage(ctx) {
    const stats = await api("/dashboard/employer", {signal: ctx.signal});

    const topJobRows = stats.topJobs.map(job => `
        <article class="application-card">
            <div>
                <h3><a href="#/job/${job.jobId}">${esc(job.title)}</a></h3>
                <p class="metadata">
                    <span class="badge ${job.isOpen ? "open" : "closed"}">${job.isOpen ? "Đang mở tuyển" : "Đã đóng"}</span>
                    · <strong>${job.applicationCount}</strong> hồ sơ đã nộp
                </p>
            </div>
            <div class="actions">
                <a class="primary small" href="#/employer/${job.jobId}/applications">Xem hồ sơ</a>
                <button type="button" class="secondary small" data-export="${job.jobId}">${icon("download")} Xuất CSV</button>
            </div>
        </article>`).join("");

    ctx.show(`
        <header class="page-intro">
            <h1>Thống kê tuyển dụng</h1>
            <p>Tổng quan hiệu quả tuyển dụng và tình hình nộp hồ sơ của doanh nghiệp bạn.</p>
        </header>

        <section class="kpi-grid">
            <div class="kpi-card">
                <span class="kpi-icon">${icon("work")}</span>
                <div>
                    <span class="kpi-label">Tin đang mở tuyển</span>
                    <strong class="kpi-value">${stats.openJobs} <span class="muted small">/ ${stats.totalJobs} tin</span></strong>
                </div>
            </div>
            <div class="kpi-card">
                <span class="kpi-icon">${icon("file")}</span>
                <div>
                    <span class="kpi-label">Tổng hồ sơ nhận được</span>
                    <strong class="kpi-value">${stats.totalApplications}</strong>
                </div>
            </div>
            <div class="kpi-card">
                <span class="kpi-icon">${icon("clock")}</span>
                <div>
                    <span class="kpi-label">Hồ sơ chờ duyệt</span>
                    <strong class="kpi-value">${stats.pendingApplications}</strong>
                </div>
            </div>
            <div class="kpi-card">
                <span class="kpi-icon">${icon("check")}</span>
                <div>
                    <span class="kpi-label">Đã duyệt nhận</span>
                    <strong class="kpi-value">${stats.acceptedApplications}</strong>
                </div>
            </div>
        </section>

        <section class="panel" style="margin-top: 24px;">
            <div class="actions balance" style="margin-bottom: 16px;">
                <h2>${icon("sort")} Tin tuyển dụng thu hút nhiều ứng viên nhất</h2>
                <a class="secondary small" href="#/employer">Xem tất cả tin</a>
            </div>
            <div class="application-list">
                ${topJobRows || '<p class="muted">Chưa có tin tuyển dụng nào có hồ sơ nộp.</p>'}
            </div>
        </section>`);

    const list = document.querySelector(".application-list");
    if (list) {
        list.onclick = async event => {
            const btn = event.target.closest("button[data-export]");
            if (!btn) return;
            const jobId = Number(btn.dataset.export);
            btn.disabled = true;
            try {
                const blob = await api(`/jobs/${jobId}/applications/export`, {blob: true});
                const url = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = `ung-vien-${jobId}.csv`;
                a.click();
                URL.revokeObjectURL(url);
                notify("Đã tải xuống danh sách ứng viên (file CSV).");
            } catch (err) {
                notify(err.message);
            } finally {
                btn.disabled = false;
            }
        };
    }
}
