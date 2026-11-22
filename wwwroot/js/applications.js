import {api, user} from "./api.js";
import {back, badge, confirmAction, date, empty, errorBox, esc, go, icon, notify, pageOf, pager, statuses, url} from "./ui.js";

export async function applicationsPage(ctx, jobId) {
    const route = jobId ? `/employer/${jobId}/applications` : "/applications";
    const status = Object.hasOwn(statuses, ctx.query.get("status")) ? ctx.query.get("status") : "";
    const page = pageOf(ctx.query);
    const path = jobId ? `/jobs/${jobId}/applications` : "/applications/mine";
    const [result, job] = await Promise.all([
        api(url(path, {status,page,pageSize:8}), {signal:ctx.signal}),
        jobId ? api(`/jobs/${jobId}`,{signal:ctx.signal}) : null
    ]);
    const last = Math.max(1,Math.ceil(result.totalCount / result.pageSize));
    if (page > last) { location.replace("#" + url(route, {status,page:last})); return; }
    const chips = Object.entries({"":"Tất cả", ...statuses}).map(([value,label]) =>
        `<a class="chip ${status === value ? "current" : ""}" href="#${esc(url(route,{status:value}))}" ${status === value ? 'aria-current="true"' : ""}>${label}</a>`).join("");
    const listing = result.items.map(item =>
        `<article class="application-card"><div><h3><a href="#/applications/${item.id}">${esc(jobId ? item.candidateName : item.jobTitle)}</a></h3>
        <p class="metadata">${jobId ? esc(item.candidateEmail) + " · " : ""}Nộp ngày ${date(item.submittedAt)}</p></div>
        <div class="actions">${badge(item.status)}<a class="secondary small" href="#/applications/${item.id}">Xem đơn</a></div></article>`).join("");
    const noResults = empty(status ? "Không có đơn ở trạng thái này" : "Chưa có đơn ứng tuyển",
        status ? "Chọn Tất cả để xem các trạng thái khác." : jobId ? "Hồ sơ sẽ xuất hiện khi có ứng viên nộp đơn." : "Hãy chọn một vị trí phù hợp và gửi CV.",
        status ? `<a class="secondary" href="#${route}">Tất cả đơn</a>` : jobId ? "" : '<a class="primary" href="#/jobs">Tìm việc</a>');
    ctx.show(`${jobId ? back(`/job/${jobId}`,"Chi tiết tin") : ""}
      <header class="page-intro"><h1>${jobId ? "Hồ sơ ứng tuyển" : "Đơn đã nộp"}</h1>${job ? `<p>${esc(job.title)}</p>` : ""}</header>
      <div class="toolbar"><div class="chips">${chips}</div><span class="count">${result.totalCount} đơn</span></div>
      <div class="application-list">${listing || noResults}</div>${pager(result,route,{status})}`);
}

export async function applicationPage(ctx, id) {
    const app = await api(`/applications/${id}`,{signal:ctx.signal});
    const employer = user.role === "Employer";
    const review = employer && app.status === "Submitted";
    ctx.show(`<div class="narrow">${back(employer ? `/employer/${app.jobId}/applications` : "/applications",employer ? "Hồ sơ ứng tuyển" : "Đơn đã nộp")}
      <article class="panel stack"><div class="actions balance"><span class="muted">Đơn #${app.id}</span>${badge(app.status)}</div>
      <h1>${esc(app.jobTitle)}</h1><div><strong>${esc(app.candidateName)}</strong><p class="muted">${esc(app.candidateEmail)}</p></div>
      <p class="metadata"><span>${icon("clock")}Nộp lúc ${date(app.submittedAt,true)}</span></p>
      <h2>Lời nhắn</h2><p class="body-copy">${esc(app.message)}</p>
      <div><button class="secondary" type="button" id="download">${icon("file")}Tải CV PDF</button></div>
      ${review ? '<h2>Xét hồ sơ</h2><div id="review" class="actions"><button type="button" data-value="Accepted" class="primary">Chấp nhận</button><button type="button" data-value="Rejected" class="danger">Từ chối</button></div>' : ""}
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
}
