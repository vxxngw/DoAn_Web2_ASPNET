import {api, user} from "./api.js";
import {back, bindForm, confirmAction, errorBox, esc, go, notify} from "./ui.js";

function localDateInput(value) {
    if (!value) return "";
    const time = new Date(value);
    return new Date(time.getTime() - time.getTimezoneOffset()*60000).toISOString().slice(0,19);
}

export async function jobForm(ctx, id) {
    const job = id ? await api(`/jobs/${id}`, {signal:ctx.signal}) : null;
    const admin = user.role === "Admin";
    const listPath = admin ? "/admin/jobs" : "/employer";
    if (job && !admin && job.employerId !== user.id) throw new Error("Bạn chỉ có thể sửa tin do mình đăng.");
    const deadline = localDateInput(job?.deadline);
    ctx.show(`<div class="narrow">${back(listPath,admin ? "Quản lý tin tuyển dụng" : "Tin của tôi")}
      <div class="panel stack"><h1>${job ? "Chỉnh sửa tin" : "Đăng tin mới"}</h1>
      <form id="job-form" class="stack"><label class="field">Tên vị trí<input name="title" required maxlength="150" value="${esc(job?.title)}"></label>
      <label class="field">Địa điểm làm việc<input name="location" required maxlength="150" value="${esc(job?.location)}"></label>
      <label class="field">Mô tả công việc<textarea name="description" required maxlength="4000" rows="7">${esc(job?.description)}</textarea></label>
      <label class="field">Hạn nộp hồ sơ<input type="datetime-local" name="deadline" required step="1" value="${deadline}">
        <span class="hint">Theo giờ địa phương của thiết bị.</span></label>
      <div class="salary-grid">
        <label class="field">Lương tối thiểu (VNĐ)
          <input type="number" name="minSalary" step="500000" min="0" placeholder="VD: 15000000" value="${job?.minSalary ?? ""}">
        </label>
        <label class="field">Lương tối đa (VNĐ)
          <input type="number" name="maxSalary" step="500000" min="0" placeholder="VD: 25000000" value="${job?.maxSalary ?? ""}">
        </label>
      </div>
      <span class="hint" style="margin-top:-6px;">Để trống nếu là lương thỏa thuận.</span>
      <label class="check"><input type="checkbox" name="isOpen" ${!job || job.isOpen ? "checked" : ""}>Mở nhận hồ sơ</label>
      ${errorBox}<div class="actions"><button class="primary" type="submit">${job ? "Lưu thay đổi" : "Đăng tin"}</button>
        ${job ? '<button class="secondary" id="reload-job" type="button" hidden>Tải lại phiên bản mới</button>' : ""}
        <a class="secondary" href="#${listPath}">Hủy</a></div></form>
      ${job ? '<div><button id="delete-job" class="danger" type="button">Xóa tin</button></div>' : ""}</div></div>`);
    bindForm("job-form", async data => {
        const inputDate = new Date(data.get("deadline"));
        const isOpen = data.has("isOpen");
        if (isOpen && inputDate.getTime() <= Date.now()) throw new Error("Hạn nộp của tin đang mở phải ở tương lai.");
        const minSalaryVal = data.get("minSalary")?.trim();
        const maxSalaryVal = data.get("maxSalary")?.trim();
        const minSalary = minSalaryVal ? Number(minSalaryVal) : null;
        const maxSalary = maxSalaryVal ? Number(maxSalaryVal) : null;
        if (minSalary !== null && maxSalary !== null && minSalary > maxSalary) {
            throw new Error("Lương tối thiểu không được lớn hơn lương tối đa.");
        }
        const body = {title:data.get("title"), location:data.get("location"), description:data.get("description"),
            minSalary, maxSalary,
            deadline:job && localDateInput(inputDate) === deadline ? job.deadline : inputDate.toISOString(), isOpen};
        if (job) body.rowVersion = job.rowVersion;
        try {
            const result = await api(job ? `/jobs/${id}` : "/jobs", {method:job ? "PUT" : "POST",body});
            if (ctx.signal.aborted) return;
            notify(job ? "Đã lưu thay đổi." : "Đã tạo tin tuyển dụng.");
            go(`/job/${id || result.id}`);
        } catch (error) {
            if (job && error.status === 409 && !ctx.signal.aborted) document.querySelector("#reload-job").hidden = false;
            throw error;
        }
    });
    if (job) {
        document.querySelector("#reload-job").onclick = () => go(`${listPath}/${id}/edit`);
        document.querySelector("#delete-job").onclick = async event => {
            const button = event.currentTarget;
            if (!await confirmAction("Xóa tin tuyển dụng?", job.title, "Xóa tin") || ctx.signal.aborted) return;
            button.disabled = true;
            try {
                await api(`/jobs/${id}`, {method:"DELETE"});
                if (!ctx.signal.aborted) { notify("Đã xóa tin."); go(listPath); }
            } catch (error) {
                if (!ctx.signal.aborted) document.querySelector(".form-error").textContent = error.message;
            } finally { button.disabled = false; }
        };
    }
}
