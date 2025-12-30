const BASE_URL = import.meta.env.VITE_API_BASE_URL;

async function request(path, options = {}) {
    const response = await fetch(`${BASE_URL}${path}`, {
        headers: {
            "Content-Type": "application/json",
            ...options.headers,
        },
        ...options,
    });

    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || `HTTP ${response.status}`);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

export function apiGet(path) {
    return request(path);
}

export function apiPost(path, body) {
    return request(path, {
        method: "POST",
        body: JSON.stringify(body),
    });
}

export function apiPatch(path, body) {
    return request(path, {
        method: "PATCH",
        body: JSON.stringify(body),
    });
}
