import {api, setUser} from "./api.js";
import {cities} from "./jobs.js";
import {bindForm, confirmAction, date, errorBox, esc, go, icon, notify, saveBlob} from "./ui.js";

const levels = ["Thực tập sinh/Sinh viên", "Mới tốt nghiệp", "Nhân viên", "Trưởng nhóm/Giám sát", "Quản lý", "Giám đốc"];
const degrees = ["Trung học", "Trung cấp", "Cao đẳng", "Cử nhân", "Kỹ sư", "Thạc sĩ", "Tiến sĩ", "Khác"];
const languageLevels = ["Sơ cấp", "Trung cấp", "Cao cấp", "Bản địa"];
const dong = new Intl.NumberFormat("vi-VN");
const month = value => `${value.slice(5, 7)}/${value.slice(0, 4)}`;
const period = item => `${month(item.from)} – ${item.to ? month(item.to) : "Hiện tại"}`;

// Mỗi trường: [tên trong API, nhãn, kiểu input, {required, length, max, options}].
const groups = {
    contact: {title: "Thông tin liên hệ", fields: [
        ["professionalTitle", "Chức danh", "text", {required: true, length: 150}],
        ["yearsOfExperience", "Số năm kinh nghiệm", "number", {max: 50}],
        ["currentLevel", "Cấp bậc hiện tại", "select", {options: levels}],
        ["highestDegree", "Bằng cấp cao nhất", "select", {options: degrees}],
        ["phone", "Số điện thoại", "tel", {length: 20}],
        ["address", "Địa chỉ", "text", {length: 250}]]},
    desired: {title: "Công việc mong muốn", fields: [
        ["desiredLocation", "Nơi làm việc", "select", {options: cities}],
        ["desiredSalary", "Mức lương mong muốn (₫/tháng)", "number", {}]]},
    objective: {title: "Mục tiêu nghề nghiệp", fields: [
        ["profileSummary", "Mục tiêu nghề nghiệp", "textarea", {length: 1500}]]}
};
// Ba trường này lưu trên tài khoản qua /auth/me/candidate-profile, phần còn lại qua /profile.
const accountKeys = ["professionalTitle", "profileSummary", "isProfilePublic"];

const lists = {
    experiences: {title: "Kinh nghiệm làm việc", noun: "kinh nghiệm", name: e => e.title, fields: [
        ["title", "Chức danh", "text", {required: true, length: 100}],
        ["company", "Công ty", "text", {required: true, length: 150}],
        ["from", "Từ tháng", "month", {required: true}],
        ["to", "Đến tháng (để trống nếu vẫn đang làm)", "month", {}],
        ["description", "Mô tả công việc", "textarea", {length: 1000}]],
        show: e => `<strong>${esc(e.title)}</strong><span>${esc(e.company)}</span><span class="muted">${period(e)}</span>
          ${e.description ? `<p class="body-copy">${esc(e.description)}</p>` : ""}`},
    educations: {title: "Học vấn", noun: "học vấn", name: e => e.school, fields: [
        ["school", "Trường", "text", {required: true, length: 150}],
        ["major", "Chuyên ngành", "text", {length: 150}],
        ["from", "Từ tháng", "month", {required: true}],
        ["to", "Đến tháng (để trống nếu đang học)", "month", {}]],
        show: e => `<strong>${esc(e.school)}</strong>${e.major ? `<span>${esc(e.major)}</span>` : ""}<span class="muted">${period(e)}</span>`},
    skills: {title: "Kỹ năng", noun: "kỹ năng", name: s => s.name, fields: [
        ["name", "Kỹ năng", "text", {required: true, length: 50}],
        ["level", "Mức thành thạo (0–5)", "number", {required: true, max: 5}]],
        show: s => `<strong>${esc(s.name)}</strong><span class="muted">${s.level}/5</span>`},
    languages: {title: "Ngoại ngữ", noun: "ngoại ngữ", name: l => l.name, fields: [
        ["name", "Ngôn ngữ", "text", {required: true, length: 50}],
        ["level", "Trình độ", "select", {required: true, options: languageLevels}]],
        show: l => `<strong>${esc(l.name)}</strong><span class="muted">${esc(l.level)}</span>`}
};

// [nhãn, đã có chưa, thuộc tính nút mở đúng chỗ bổ sung]
const checks = [
    ["Ảnh đại diện", (p, account) => account.avatarDataUrl, "data-avatar"],
    ["Thông tin liên hệ", (p, account) => account.professionalTitle && p.phone && p.address, 'data-group="contact"'],
    ["Công việc mong muốn", p => p.desiredLocation && p.desiredSalary != null, 'data-group="desired"'],
    ["Mục tiêu nghề nghiệp", (p, account) => account.profileSummary, 'data-group="objective"'],
    ["Kinh nghiệm", p => p.experiences.length, 'data-add="experiences"'],
    ["Học vấn", p => p.educations.length, 'data-add="educations"'],
    ["Kỹ năng", p => p.skills.length, 'data-add="skills"'],
    ["Ngoại ngữ", p => p.languages.length, 'data-add="languages"'],
    ["CV đính kèm", (p, account, cv) => cv, "data-setup"]
];

function fieldsMarkup(fields, values) {
    return fields.map(([name, label, type, {required, length, max, options}]) => {
        const value = values[name] ?? "";
        const attributes = `name="${name}" ${required ? "required" : ""} ${length ? `maxlength="${length}"` : ""}
            ${type === "number" ? `min="0" step="1" ${max ? `max="${max}"` : ""}` : ""}`;
        const control = type === "textarea" ? `<textarea ${attributes}>${esc(value)}</textarea>`
            : type === "select" ? `<select ${attributes}><option value="">Chọn</option>
                ${options.map(option => `<option ${option === value ? "selected" : ""}>${esc(option)}</option>`).join("")}</select>`
            : `<input type="${type}" ${attributes} value="${esc(type === "month" ? String(value).slice(0, 7) : value)}">`;
        return `<label class="field">${label}${control}</label>`;
    }).join("");
}

function readFields(fields, form) {
    return Object.fromEntries(fields.map(([name, , type]) => {
        const raw = form.elements.namedItem(name).value.trim();
        return [name, !raw ? null : type === "number" ? Number(raw) : type === "month" ? raw + "-01" : raw];
    }));
}

function head(id, title, button) {
    return `<div class="section-head"><h2 id="${id}">${title}</h2>${button}</div>`;
}

function editButton(key) {
    return `<button type="button" class="secondary small" data-group="${key}" aria-label="Chỉnh sửa ${groups[key].title.toLowerCase()}">Chỉnh sửa</button>`;
}

function factsPanel(key, rows) {
    return `<section class="panel stack" aria-labelledby="${key}-title">${head(`${key}-title`, groups[key].title, editButton(key))}
      <dl class="profile-facts">${rows.map(([label, value]) => `<div><dt>${label}</dt>
        <dd>${value == null || value === "" ? '<span class="muted">Chưa cập nhật</span>' : esc(value)}</dd></div>`).join("")}</dl></section>`;
}

function listPanel(key, items) {
    const list = lists[key];
    const rows = items.map((item, i) => `<li><div>${list.show(item)}</div><div class="actions">
        <button type="button" class="text-action small" data-edit="${key}" data-index="${i}" aria-label="Sửa ${list.noun} ${esc(list.name(item))}">Sửa</button>
        <button type="button" class="danger small" data-remove="${key}" data-index="${i}" aria-label="Xóa ${list.noun} ${esc(list.name(item))}">Xóa</button></div></li>`);
    return `<section class="panel stack" aria-labelledby="${key}-title">
      ${head(`${key}-title`, list.title, `<button type="button" class="secondary small" data-add="${key}">${icon("plus")}Thêm ${list.noun}</button>`)}
      ${items.length ? `<ul class="profile-items">${rows.join("")}</ul>` : `<p class="muted">Chưa có ${list.noun}.</p>`}</section>`;
}

function level(percent) {
    return percent < 50 ? "Cơ bản" : percent < 75 ? "Trung bình" : percent < 100 ? "Tương đối hoàn chỉnh" : "Hoàn chỉnh";
}

export function candidateSections(account, {profile: p, cv}) {
    const missing = checks.filter(([, test]) => !test(p, account, cv));
    const percent = Math.round((checks.length - missing.length) / checks.length * 100);
    return `<div id="candidate-profile" class="stack profile-sections">
      <section class="panel stack" aria-labelledby="completion-title">
        ${head("completion-title", "Hoàn chỉnh hồ sơ", '<button type="button" class="primary small" data-setup>Thiết lập hồ sơ</button>')}
        <progress max="100" value="${percent}" aria-labelledby="completion-title" aria-describedby="completion-level"></progress>
        <p id="completion-level"><strong>${percent}%</strong> · ${level(percent)}</p>
        ${missing.length ? `<p class="muted">Bổ sung các mục sau để nhà tuyển dụng hiểu rõ bạn hơn:</p>
          <div class="chips">${missing.map(([label, , action]) => `<button type="button" class="chip" ${action}>${icon("plus")}${label}</button>`).join("")}</div>` : ""}
        <label class="check"><input type="checkbox" id="profile-public" ${account.isProfilePublic ? "checked" : ""} aria-describedby="public-hint">Cho phép nhà tuyển dụng tìm thấy hồ sơ của tôi</label>
        <p class="hint" id="public-hint">Khi bật, nhà tuyển dụng có thể xem họ tên, email, chức danh và mục tiêu nghề nghiệp. Chỉ công ty nhận đơn mới xem được CV bạn đã gửi.</p>
      </section>
      ${factsPanel("contact", [["Chức danh", account.professionalTitle], ["Kinh nghiệm", p.yearsOfExperience != null ? `${p.yearsOfExperience} năm` : null],
        ["Cấp bậc hiện tại", p.currentLevel], ["Bằng cấp cao nhất", p.highestDegree], ["Số điện thoại", p.phone], ["Địa chỉ", p.address]])}
      ${factsPanel("desired", [["Nơi làm việc", p.desiredLocation],
        ["Mức lương mong muốn", p.desiredSalary != null ? `${dong.format(p.desiredSalary)} ₫/tháng` : null]])}
      <section class="panel stack" aria-labelledby="cv-title">
        ${head("cv-title", "Hồ sơ đính kèm", `<button type="button" class="secondary small" data-setup>${icon("plus")}${cv ? "Tải CV khác" : "Tải CV"}</button>`)}
        ${cv ? `<p class="actions"><button type="button" class="text-action small" id="download-cv">${icon("file")}${esc(cv.fileName)}</button>
          <span class="muted">Tải lên: ${date(cv.uploadedAt)}</span></p>` : '<p class="muted">Chưa có CV đính kèm.</p>'}
      </section>
      <section class="panel stack" aria-labelledby="objective-title">${head("objective-title", groups.objective.title, editButton("objective"))}
        ${account.profileSummary ? `<p class="body-copy">${esc(account.profileSummary)}</p>` : '<p class="muted">Chưa cập nhật.</p>'}</section>
      ${Object.keys(lists).map(key => listPanel(key, p[key])).join("")}
      <dialog id="profile-editor" class="profile-dialog" aria-labelledby="editor-title"></dialog></div>`;
}

// Chỉ gọi API của phần có thay đổi. Hai lệnh gọi không cùng giao dịch nhưng mỗi phần tự lưu trọn vẹn.
async function persist(ctx, account, profile, changes) {
    const keys = Object.keys(changes);
    if (keys.some(key => accountKeys.includes(key))) {
        const {isProfilePublic, professionalTitle, profileSummary} = {...account, ...changes};
        const updated = await api("/auth/me/candidate-profile",
            {method: "PUT", body: {isProfilePublic, professionalTitle, profileSummary}, signal: ctx.signal});
        Object.assign(account, updated);
        setUser(updated);
    }
    if (keys.some(key => !accountKeys.includes(key)))
        await api("/profile", {method: "PUT", body: {...profile, ...changes}, signal: ctx.signal});
}

function openEditor(dialog, title, fields, values, submit) {
    dialog.innerHTML = `<form id="profile-editor-form" class="stack"><h2 id="editor-title">${esc(title)}</h2>
      ${fieldsMarkup(fields, values)}${errorBox}
      <div class="actions"><button type="submit" class="primary">Lưu</button><button type="button" class="secondary" data-close>Hủy</button></div></form>`;
    dialog.querySelector("[data-close]").onclick = () => dialog.close();
    bindForm("profile-editor-form", (_, form) => submit(readFields(fields, form)));
    dialog.showModal();
}

function openSetup(ctx, dialog, account, {profile, cv}, next) {
    const steps = ["Tải hồ sơ", "Thông tin liên hệ", "Hoàn chỉnh hồ sơ"];
    const draft = {};
    let step = 0;
    const show = () => {
        const fields = step === 1 ? groups.contact.fields : [...groups.desired.fields, ...groups.objective.fields];
        dialog.innerHTML = `<form id="setup-form" class="stack"><h2 id="editor-title">Thiết lập hồ sơ</h2>
          <ol class="setup-steps">${steps.map((name, i) => `<li class="${i < step ? "done" : ""}" ${i === step ? 'aria-current="step"' : ""}>
            <span>${i < step ? icon("check") : i + 1}</span>${name}</li>`).join("")}</ol>
          ${step === 0 ? `<label class="cv-drop">${icon("file")}<strong>Kéo CV vào đây hoặc chọn từ máy</strong>
            <input type="file" name="cv" accept=".pdf,.doc,.docx" aria-describedby="cv-hint">
            <span class="hint" id="cv-hint">Hỗ trợ .pdf, .doc, .docx dưới 5120 KB.${cv ? ` CV hiện tại: ${esc(cv.fileName)}.` : ""} Có thể bỏ qua bước này.</span></label>`
            : fieldsMarkup(fields, {...account, ...profile, ...draft})}
          ${errorBox}<div class="actions"><button type="submit" class="primary">${step === 2 ? "Hoàn tất" : "Tiếp tục"}</button>
          ${step ? '<button type="button" class="secondary" data-back>Quay lại</button>' : ""}
          <button type="button" class="text-action" data-close>Để sau</button></div></form>`;
        const form = dialog.querySelector("form");
        dialog.querySelector("[data-close]").onclick = () => dialog.close();
        if (step) dialog.querySelector("[data-back]").onclick = () => {
            Object.assign(draft, readFields(fields, form));
            step--;
            show();
        };
        bindForm("setup-form", async data => {
            if (step === 0) {
                const file = data.get("cv");
                if (file.size > 5 * 1024 * 1024) throw new Error("CV vượt quá 5 MiB.");
                if (file.size) await api("/profile/cv", {method: "PUT", body: data, signal: ctx.signal});
            } else Object.assign(draft, readFields(fields, form));
            if (step < 2) {
                step++;
                show();
                return;
            }
            await persist(ctx, account, profile, draft);
            notify("Đã thiết lập hồ sơ.");
            dialog.close();
        });
    };
    // Đóng bằng Esc, "Để sau" hay "Hoàn tất" đều tải lại trang để thấy dữ liệu mới.
    dialog.onclose = () => {
        dialog.onclose = null;
        if (!ctx.signal.aborted) go(next || "/profile");
    };
    show();
    dialog.showModal();
}

export function bindCandidateProfile(ctx, account, data, setupNext) {
    const {profile, cv} = data;
    const container = document.querySelector("#candidate-profile");
    const dialog = container.querySelector("#profile-editor");
    const save = async changes => {
        await persist(ctx, account, profile, changes);
        if (ctx.signal.aborted) return;
        dialog.close();
        go("/profile");
        notify("Đã lưu hồ sơ.");
    };
    container.addEventListener("click", async event => {
        const button = event.target.closest("button");
        if (!button || dialog.contains(button)) return;
        const {group, add, edit, remove, index} = button.dataset;
        if (group) openEditor(dialog, groups[group].title, groups[group].fields, {...account, ...profile}, save);
        else if (add) openEditor(dialog, `Thêm ${lists[add].noun}`, lists[add].fields, {}, values => save({[add]: [...profile[add], values]}));
        else if (edit) openEditor(dialog, `Sửa ${lists[edit].noun}`, lists[edit].fields, profile[edit][index],
            values => save({[edit]: profile[edit].with(Number(index), values)}));
        else if (remove) {
            const list = lists[remove];
            if (!await confirmAction(`Xóa ${list.noun}`, `Xóa "${list.name(profile[remove][index])}" khỏi hồ sơ?`, "Xóa")) return;
            await save({[remove]: profile[remove].filter((_, i) => i !== Number(index))}).catch(error => notify(error.message));
        }
        else if ("setup" in button.dataset) openSetup(ctx, dialog, account, data, "");
        else if ("avatar" in button.dataset) document.querySelector("#avatar-form input[type=file]").focus();
        else if (button.id === "download-cv") {
            button.disabled = true;
            try { saveBlob(await api("/profile/cv", {blob: true, signal: ctx.signal}), cv.fileName); }
            catch (error) { if (!ctx.signal.aborted) notify(error.message); }
            finally { button.disabled = false; }
        }
    });
    const toggle = container.querySelector("#profile-public");
    toggle.onchange = async () => {
        toggle.disabled = true;
        try {
            await persist(ctx, account, profile, {isProfilePublic: toggle.checked});
            notify(toggle.checked ? "Nhà tuyển dụng đã có thể tìm thấy hồ sơ của bạn." : "Đã ẩn hồ sơ khỏi danh bạ nhà tuyển dụng.");
        } catch (error) {
            toggle.checked = !toggle.checked;
            if (!ctx.signal.aborted) notify(error.message);
        } finally { toggle.disabled = false; }
    };
    if (setupNext !== null) openSetup(ctx, dialog, account, data, setupNext);
}
