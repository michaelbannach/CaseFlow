// src/api/client.js

export const TOKEN_KEY = "caseflow_token";
const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5180";

// -------------------------
// Token helpers
// -------------------------

export function setToken(token) {
    if (!token) return;
    localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken() {
    localStorage.removeItem(TOKEN_KEY);
}

export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

// -------------------------
// Low-level request helper
// Supports JSON + FormData (multipart)
// -------------------------

async function request(method, url, body) {
    const token = getToken();

    const headers = {};
    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const isFormData = body instanceof FormData;

    // IMPORTANT:
    // - For JSON requests: set Content-Type and stringify
    // - For FormData: DO NOT set Content-Type (browser sets boundary)
    if (!isFormData && body !== undefined) {
        headers["Content-Type"] = "application/json";
    }

    const res = await fetch(`${API_BASE_URL}${url}`, {
        method,
        headers,
        body:
            body === undefined
                ? undefined
                : isFormData
                    ? body
                    : JSON.stringify(body),
    });

    // Handle non-2xx
    if (!res.ok) {
        // Try to extract a useful message from JSON error or plain text
        const contentType = res.headers.get("content-type") ?? "";
        let message = `Request failed (${res.status} ${res.statusText})`;

        try {
            if (contentType.includes("application/json")) {
                const json = await res.json();
                message =
                    json?.error ||
                    json?.message ||
                    JSON.stringify(json) ||
                    message;
            } else {
                const text = await res.text();
                if (text) message = text;
            }
        } catch {
            // ignore parsing errors; keep default message
        }

        throw new Error(message);
    }

    // No content
    if (res.status === 204) return null;

    // Return JSON only when it is JSON; otherwise return null
    const contentType = res.headers.get("content-type");
    if (contentType?.includes("application/json")) {
        return res.json();
    }

    return null;
}

export async function apiGetBlob(url) {
    const token = getToken();

    const headers = {};
    if (token) headers.Authorization = `Bearer ${token}`;

    const res = await fetch(`${API_BASE_URL}${url}`, { method: "GET", headers });

    if (!res.ok) {
        const text = await res.text();
        throw new Error(text || `Request failed (${res.status})`);
    }

    return res.blob();
}

// -------------------------
// Convenience wrappers
// -------------------------

export const apiGet = (url) => request("GET", url);
export const apiPost = (url, body) => request("POST", url, body);
export const apiPatch = (url, body) => request("PATCH", url, body);
export const apiDelete = (url) => request("DELETE", url);
