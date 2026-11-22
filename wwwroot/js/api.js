const sessionKey = "viecmo.accessToken";
export let user = null;

export function setUser(value) {
    user = value;
}

export function setSession(login) {
    sessionStorage.setItem(sessionKey, login.accessToken);
    setUser(login.user);
}

export function clearSession() {
    sessionStorage.removeItem(sessionKey);
    setUser(null);
}

export function hasSession() {
    return Boolean(sessionStorage.getItem(sessionKey));
}

export class ApiError extends Error {
    constructor(status, message) {
        super(message);
        this.status = status;
    }
}

// Gửi JWT cho API cùng origin. FormData tự tạo Content-Type kèm multipart boundary.
export async function api(path, {method = "GET", body, signal, blob = false, anonymous = false} = {}) {
    const headers = {};
    const token = !anonymous && sessionStorage.getItem(sessionKey);
    if (token) headers.Authorization = `Bearer ${token}`;
    if (body !== undefined && !(body instanceof FormData)) {
        headers["Content-Type"] = "application/json";
        body = JSON.stringify(body);
    }

    let response;
    try {
        response = await fetch("/api" + path, {method, body, headers, signal, cache: "no-store"});
    } catch (error) {
        if (error.name === "AbortError") throw error;
        throw new ApiError(0, "Không kết nối được máy chủ. Hãy kiểm tra kết nối rồi thử lại.");
    }
    if (!response.ok) {
        const problem = await response.json().catch(() => ({}));
        if (response.status === 401 && token && token === sessionStorage.getItem(sessionKey)) {
            clearSession();
            window.dispatchEvent(new Event("session-expired"));
        }
        const details = Object.values(problem.errors || {}).flat().join(" ");
        throw new ApiError(response.status, details || problem.title || `Yêu cầu thất bại (HTTP ${response.status}).`);
    }
    if (response.status === 204) return null;
    return blob ? response.blob() : response.json();
}
