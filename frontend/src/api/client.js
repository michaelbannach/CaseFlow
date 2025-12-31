export const TOKEN_KEY = "caseflow_token";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5180";

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

async function request(method, url, body) {
    const headers = {
        "Content-Type": "application/json",
    };

    const token = getToken();
    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const res = await fetch(`${API_BASE_URL}${url}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
    });

    
    if (!res.ok) {
        let message = "Request failed";
        try {
            const text = await res.text();
            if (text) message = text;
        } catch {
            /* ignore */
        }
        throw new Error(message);
    }

    if (res.status === 204) return null;

    const contentType = res.headers.get("content-type");
    if (contentType?.includes("application/json")) {
        return res.json();
    }

    return null;
}

export const apiGet = (url) => request("GET", url);
export const apiPost = (url, body) => request("POST", url, body);
export const apiPatch = (url, body) => request("PATCH", url, body);
