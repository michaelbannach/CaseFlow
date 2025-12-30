const API_BASE = import.meta.env.VITE_API_BASE_URL;

export async function api(path, options = {}) {
    const token = localStorage.getItem("caseflow_token");

    const response = await fetch(`${API_BASE}${path}`, {
        ...options,
        headers: {
            ...(options.headers || {}),
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
    });

    if (response.status === 401) {
        localStorage.removeItem("caseflow_token");
        window.location.href = "/login";
        return;
    }

    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || `HTTP ${response.status}`);
    }

    // Falls mal 204 No Content zurückkommt:
    if (response.status === 204) return null;

    return response.json();
}
