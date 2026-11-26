import {api, user} from "./api.js";
import {esc, go, icon, notify} from "./ui.js";

export function saveButton(job) {
    if (user?.role !== "Candidate") return "";
    const label = job.isSaved ? "Bỏ lưu" : "Lưu tin";
    return `<button type="button" class="save-job" data-job="${job.id}" data-title="${esc(job.title)}"
        aria-label="${label} · ${esc(job.title)}" data-saved="${Boolean(job.isSaved)}">${icon("bookmark")}<span>${label}</span></button>`;
}

export async function toggleSaved(button) {
    if (button.disabled) return;
    const save = button.dataset.saved !== "true";
    const accountId = user?.id;
    button.disabled = true;
    try {
        await api(`/saved-jobs/${button.dataset.job}`, {method:save ? "PUT" : "DELETE"});
        if (user?.id !== accountId) return;
        const label = save ? "Bỏ lưu" : "Lưu tin";
        for (const match of document.querySelectorAll(`.save-job[data-job="${button.dataset.job}"]`)) {
            match.dataset.saved = String(save);
            match.setAttribute("aria-label", `${label} · ${match.dataset.title}`);
            match.querySelector("span").textContent = label;
        }
        notify(save ? "Đã thêm vào Việc đã lưu." : "Đã bỏ lưu tin.");
        if (location.hash.split("?")[0] === "#/saved-jobs") go(location.hash.slice(1));
    } catch (error) {
        if (button.isConnected) notify(error.message);
    } finally { button.disabled = false; }
}
