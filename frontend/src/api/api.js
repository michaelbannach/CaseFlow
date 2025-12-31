// src/api/client.js
const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5180";
export const TOKEN_KEY = "caseflow_jwt";

function getAuthHeader() {
    const token = localStorage.getItem(TOKEN_KEY);
    return token ? { Authorization: `Bearer ${token}` } : {};
}

async function request(path, options = {}) {
    const url = `${API_BASE}${path}`;

    const headers = {
        ...(options.headers ?? {}),
        ...getAuthHeader(),
    };

    const resp = await fetch(url, { ...options, headers });

    // Optional: Auto-Logout bei 401
    if (resp.status === 401) {
        localStorage.removeItem(TOKEN_KEY);
    }

    // NoContent
    if (resp.status === 204) return null;

    const contentType = resp.headers.get("content-type") ?? "";
    const isJson = contentType.includes("application/json");

    if (!resp.ok) {
        let msg = `Request failed (${resp.status})`;
        try {
            const body = isJson ? await resp.json() : await resp.text();
            msg = body?.error ?? body?.title ?? body ?? msg;
        } catch {
            // ignore
        }
        throw new Error(msg);
    }

    return isJson ? resp.json() : resp.text();
}

export function apiGet(path) {
    return request(path, { method: "GET" });
}

export function apiPost(path, body) {
    return request(path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body ?? {}),
    });
}

export function apiPatch(path, body) {
    return request(path, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body ?? {}),
    });
}
