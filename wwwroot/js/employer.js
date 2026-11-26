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
    if (job?.archived && !admin) { go("/employer/report"); return; }
    const deadline = localDateInput(job?.deadline);
    const industries = await api("/jobs/industries",{signal:ctx.signal});
    ctx.show(`<div class="narrow">${back(listPath,admin ? "Quản lý tin tuyển dụng" : "Tin của tôi")}
      <div class="panel stack"><h1>${job ? "Chỉnh sửa tin" : "Đăng tin mới"}</h1>
      <form id="job-form" class="stack"><label class="field">Tên vị trí<input name="title" required maxlength="150" value="${esc(job?.title)}"></label>
      <label class="field">Địa điểm làm việc<input name="location" required maxlength="150" value="${esc(job?.location)}"></label>
      <label class="field">Ngành nghề<input name="industry" list="industry-options" maxlength="100" value="${esc(job?.industry)}" placeholder="Chọn hoặc nhập tên ngành"></label>
      <datalist id="industry-options">${industries.map(i => `<option value="${esc(i.industry)}"></option>`).join("")}</datalist>
      <label class="field">Mức lương theo tháng<select name="salaryMode" ${job && job.isSalaryNegotiable == null ? "" : "required"}>
        <option value="">${job && job.isSalaryNegotiable == null ? "Chưa cung cấp" : "Chọn hình thức lương"}</option>
        <option value="range" ${job?.isSalaryNegotiable === false ? "selected" : ""}>Khoảng lương</option>
        <option value="negotiable" ${job?.isSalaryNegotiable === true ? "selected" : ""}>Thỏa thuận</option>
      </select></label>
      <fieldset id="salary-range" class="salary-range" hidden disabled><legend class="sr-only">Khoảng lương theo tháng</legend>
        <label class="field">Tối thiểu (VNĐ)<input type="number" name="salaryMin" min="1" max="2147483647" step="1" required value="${job?.salaryMin ?? ""}"></label>
        <label class="field">Tối đa (VNĐ)<input type="number" name="salaryMax" min="1" max="2147483647" step="1" required value="${job?.salaryMax ?? ""}"></label>
      </fieldset>
      <label class="field">Mô tả công việc<textarea name="description" required maxlength="4000" rows="7">${esc(job?.description)}</textarea></label>
      <fieldset class="stack jd-fields"><legend>Bản mô tả công việc (JD)</legend>
        <label class="field">Hình thức đính kèm<select name="jdMode">
          ${job?.hasJdPdf || job?.jdUrl ? `<option value="keep">Giữ ${job.hasJdPdf ? "tệp PDF" : "đường dẫn"} hiện tại</option>` : ""}
          <option value="none">Không đính kèm JD</option><option value="url">Đường dẫn</option><option value="pdf">Tệp PDF</option>
        </select></label>
        <label class="field" hidden>Đường dẫn JD<input name="jdUrl" type="url" maxlength="2048" value="${esc(job?.jdUrl)}" placeholder="https://"></label>
        <label class="field" hidden>Chọn tệp JD (PDF, tối đa 5 MiB)<input name="pdf" type="file" accept=".pdf,application/pdf"></label>
      </fieldset>
      <label class="field">Hạn nộp hồ sơ<input type="datetime-local" name="deadline" required step="1" value="${deadline}">
        <span class="hint">Theo giờ địa phương của thiết bị.</span></label>
      <label class="check"><input type="checkbox" name="isOpen" ${!job || job.isOpen ? "checked" : ""} ${job?.archived ? "disabled" : ""}>${job?.archived ? "Tin đã lưu trữ" : "Mở nhận hồ sơ"}</label>
      ${errorBox}<div class="actions"><button class="primary" type="submit">${job ? "Lưu thay đổi" : "Đăng tin"}</button>
        ${job ? '<button class="secondary" id="reload-job" type="button" hidden>Tải lại phiên bản mới</button>' : ""}
        <a class="secondary" href="#${listPath}">Hủy</a></div></form>
      ${job ? '<div><button id="delete-job" class="danger" type="button">Xóa tin</button></div>' : ""}</div></div>`);
    const salaryMode = document.querySelector("#job-form select[name=salaryMode]");
    const range = document.getElementById("salary-range");
    function showSalaryRange() {
        range.hidden = range.disabled = salaryMode.value !== "range";
    }
    salaryMode.addEventListener("change", showSalaryRange);
    showSalaryRange();
    const fields = document.getElementById("job-form").elements;
    function showJdInput() {
        for (const [field, mode] of [[fields.jdUrl, "url"], [fields.pdf, "pdf"]]) {
            field.required = fields.jdMode.value === mode;
            field.disabled = field.closest("label").hidden = !field.required;
        }
    }
    fields.jdMode.addEventListener("change", showJdInput);
    showJdInput();
    bindForm("job-form", async data => {
        const inputDate = new Date(data.get("deadline"));
        const isOpen = data.has("isOpen");
        if (isOpen && inputDate.getTime() <= Date.now()) throw new Error("Hạn nộp của tin đang mở phải ở tương lai.");
        const body = {title:data.get("title"), location:data.get("location"), industry:data.get("industry"), description:data.get("description"),
            deadline:job && localDateInput(inputDate) === deadline ? job.deadline : inputDate.toISOString(), isOpen};
        if (job) body.rowVersion = job.rowVersion;
        if (data.get("salaryMode")) body.isSalaryNegotiable = data.get("salaryMode") === "negotiable";
        if (body.isSalaryNegotiable === false) {
            body.salaryMin = Number(data.get("salaryMin"));
            body.salaryMax = Number(data.get("salaryMax"));
            if (body.salaryMax < body.salaryMin) throw new Error("Lương tối đa phải bằng hoặc cao hơn lương tối thiểu.");
        }
        try {
            let path = job ? `/jobs/${id}` : "/jobs";
            let payload = body;
            if (data.get("jdMode") === "url") body.jdUrl = data.get("jdUrl").trim();
            if (job && data.get("jdMode") === "none") body.removeJd = true;
            if (data.get("jdMode") === "pdf") {
                const file = data.get("pdf");
                if (file.size > 5 * 1024 * 1024) throw new Error("Tệp JD vượt quá 5 MiB.");
                payload = new FormData();
                for (const [key, value] of Object.entries(body)) payload.append(key, value);
                payload.append("Pdf", file);
                path += "/with-jd";
            }
            const result = await api(path, {method:job ? "PUT" : "POST", body:payload, signal:ctx.signal});
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
            button.disabled = true;
            try {
                let warning = `Tin “${job.title}” sẽ được lưu trữ nếu đã có đơn. Bạn vẫn xem được các đơn và CV trong Báo cáo. Tin chưa nhận đơn sẽ được xóa vĩnh viễn.`;
                if (admin) {
                    const impact = await api(`/admin/jobs/${id}/deletion-impact`, {signal:ctx.signal});
                    warning = `Tin “${job.title}” và ${impact.applications} đơn kèm CV sẽ bị xóa vĩnh viễn.`;
                }
                if (!await confirmAction("Xóa tin tuyển dụng?", warning, "Xóa tin") || ctx.signal.aborted) return;
                await api(`/jobs/${id}`, {method:"DELETE"});
                if (!ctx.signal.aborted) { notify("Đã gỡ tin tuyển dụng."); go(listPath); }
            } catch (error) {
                if (!ctx.signal.aborted) document.querySelector(".form-error").textContent = error.message;
            } finally { button.disabled = false; }
        };
    }
}
