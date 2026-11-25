import {api, user} from "./api.js";
import {back, badge, confirmAction, date, empty, errorBox, esc, go, icon, notify, pageOf, pageSizeOf, pager, statuses, url} from "./ui.js";

export async function applicationsPage(ctx, jobId, admin = false) {
    const route = admin ? "/admin/applications" : jobId ? `/employer/${jobId}/applications` : "/applications";
    const status = Object.hasOwn(statuses, ctx.query.get("status")) ? ctx.query.get("status") : "";
    const page = pageOf(ctx.query);
    const pageSize = pageSizeOf(ctx.query, 6);
    const params = { status, pageSize };
    const path = admin ? "/applications/admin" : jobId ? `/jobs/${jobId}/applications` : "/applications/mine";
    const [result, job] = await Promise.all([
        api(url(path, {status, page, pageSize}), {signal:ctx.signal}),
        jobId ? api(`/jobs/${jobId}`,{signal:ctx.signal}) : null
    ]);
    const totalPages = result.totalPages ?? Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    if (page > totalPages) { location.replace("#" + url(route, {...params, page: totalPages})); return; }
    const chips = Object.entries({"":"Tất cả", ...statuses}).map(([value,label]) =>
        `<a class="chip ${status === value ? "current" : ""}" href="#${esc(url(route,{...params, status:value, page: 1}))}" ${status === value ? 'aria-current="true"' : ""}>${label}</a>`).join("");
    const listing = result.items.map(item =>
        `<article class="application-card"><div><h3><a href="#/applications/${item.id}">${esc(jobId ? item.candidateName : item.jobTitle)}</a></h3>
        <p class="metadata">${jobId ? esc(item.candidateEmail) + " · " : ""}Nộp ngày ${date(item.submittedAt)}</p></div>
        <div class="actions">${badge(item.status)}<a class="secondary small" href="#/applications/${item.id}">Xem đơn</a></div></article>`).join("");
    const noResults = empty(status ? "Không có đơn ở trạng thái này" : admin ? "Chưa có hồ sơ ứng tuyển" : "Chưa có đơn ứng tuyển",
        status === "Accepted" ? "Chưa có hồ sơ nào được nhà tuyển dụng chấp nhận." : status === "Rejected" ? "Chưa có hồ sơ nào bị từ chối." : status === "Submitted" ? "Chưa có hồ sơ mới đang chờ xét duyệt." : status === "Withdrawn" ? "Chưa có hồ sơ nào được ứng viên rút." : admin ? "Hồ sơ sẽ xuất hiện khi ứng viên nộp đơn." : jobId ? "Hồ sơ sẽ xuất hiện khi có ứng viên nộp đơn." : "Hãy chọn một vị trí phù hợp và gửi CV.",
        status ? `<a class="secondary" href="#${route}">Tất cả đơn</a>` : jobId ? "" : '<a class="primary" href="#/jobs">Tìm việc</a>');
    const exportBtn = jobId ? `<button type="button" class="secondary small" id="export-csv" style="display:inline-flex;align-items:center;gap:6px;">${icon("download")} Xuất file CSV</button>` : "";
    const pageSubtitle = totalPages > 1 ? ` · Trang ${result.page}/${totalPages}` : "";
    ctx.show(`${jobId ? back(`/job/${jobId}`,"Chi tiết tin") : ""}
      <header class="page-intro"><h1>${admin ? "Hồ sơ ứng tuyển toàn hệ thống" : jobId ? "Hồ sơ ứng tuyển" : "Đơn đã nộp"}</h1>${job ? `<p>${esc(job.title)}</p>` : ""}</header>
      <div class="toolbar"><div class="chips">${chips}</div><div style="display:flex;align-items:center;gap:12px;">${exportBtn}<span class="count">${result.totalCount} đơn${pageSubtitle}</span></div></div>
      <div class="application-list">${listing || noResults}</div>${pager(result,route,params)}`);

    if (jobId) {
        const btn = document.querySelector("#export-csv");
        if (btn) {
            btn.onclick = async () => {
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
}

export async function applicationPage(ctx, id) {
    const app = await api(`/applications/${id}`,{signal:ctx.signal});
    const employer = user.role === "Employer";
    const candidate = user.role === "Candidate";
    const admin = user.role === "Admin";
    const review = employer && app.status === "Submitted";
    const withdraw = candidate && app.status === "Submitted";
    ctx.show(`<div class="narrow">${back(admin ? "/admin/applications" : employer ? `/employer/${app.jobId}/applications` : "/applications",admin ? "Hồ sơ toàn hệ thống" : employer ? "Hồ sơ ứng tuyển" : "Đơn đã nộp")}
      <article class="panel stack"><div class="actions balance"><span class="muted">Đơn #${app.id}</span>${badge(app.status)}</div>
      <h1>${esc(app.jobTitle)}</h1><div><strong>${esc(app.candidateName)}</strong><p class="muted">${esc(app.candidateEmail)}</p></div>
      <p class="metadata"><span>${icon("clock")}Nộp lúc ${date(app.submittedAt,true)}</span></p>
      <h2>Lời nhắn</h2><p class="body-copy">${esc(app.message)}</p>
      <div><button class="secondary" type="button" id="download">${icon("file")}Tải CV PDF</button></div>
      ${review ? '<h2>Xét hồ sơ</h2><div id="review" class="actions"><button type="button" data-value="Accepted" class="primary">Chấp nhận</button><button type="button" data-value="Rejected" class="danger">Từ chối</button></div>' : ""}
      ${withdraw ? '<div id="withdraw" class="actions"><button type="button" class="danger">Rút đơn ứng tuyển</button></div>' : ""}
      ${errorBox}</article></div>`);
    const error = document.querySelector(".form-error");
    document.querySelector("#download").onclick = async event => {
        const button = event.currentTarget;
        button.disabled = true;
        error.textContent = "";
        try {
            const pdf = await api(`/applications/${id}/cv`, {blob:true,signal:ctx.signal});
            const address = URL.createObjectURL(pdf);
            const link = document.createElement("a");
            link.href = address;
            link.download = `cv-${id}.pdf`;
            document.body.append(link);
            link.click();
            link.remove();
            setTimeout(() => URL.revokeObjectURL(address), 30000);
        } catch (failure) {
            if (!ctx.signal.aborted) error.textContent = failure.message;
        } finally { button.disabled = false; }
    };
    if (review) document.querySelector("#review").onclick = async event => {
        const button = event.target.closest("button[data-value]");
        if (!button) return;
        const decision = button.dataset.value;
        if (!await confirmAction(statuses[decision] + " hồ sơ?", "Kết quả sẽ hiển thị cho ứng viên và chỉ được ghi một lần.", statuses[decision]) || ctx.signal.aborted) return;
        const buttons = [...document.querySelectorAll("#review button")];
        buttons.forEach(b => b.disabled = true);
        try {
            await api(`/applications/${id}/status`,{method:"PATCH",body:{status:decision}});
            if (!ctx.signal.aborted) { notify("Đã cập nhật kết quả xét hồ sơ."); go(`/applications/${id}`); }
        } catch (failure) {
            if (!ctx.signal.aborted && failure.status === 409) {
                notify(failure.message);
                go(`/applications/${id}`);
            } else error.textContent = failure.message;
        } finally { buttons.forEach(b => b.disabled = false); }
    };
    if (withdraw) document.querySelector("#withdraw").onclick = async event => {
        const button = event.currentTarget;
        if (!await confirmAction("Rút đơn ứng tuyển?", "Đơn rút sẽ không còn được nhà tuyển dụng xử lý.", "Rút đơn") || ctx.signal.aborted) return;
        button.disabled = true;
        try {
            await api(`/applications/${id}/withdraw`,{method:"PATCH"});
            if (!ctx.signal.aborted) { notify("Đã rút đơn ứng tuyển."); go(`/applications/${id}`); }
        } catch (failure) {
            if (!ctx.signal.aborted) {
                if (failure.status === 409) { notify(failure.message); go(`/applications/${id}`); }
                else error.textContent = failure.message;
            }
        } finally { button.disabled = false; }
    };
}
