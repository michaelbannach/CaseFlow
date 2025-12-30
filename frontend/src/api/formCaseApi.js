import { apiPost, apiGet, apiPatch } from "./client"; // oder dein fetch-wrapper

export async function createCase(payload) {
    return apiPost("/api/formcases", payload);
}

export async function getCases() {
    return apiGet("/api/formcases");
}

export async function getCaseById(id) {
    return apiGet(`/api/formcases/${id}`);
}

export async function setCaseStatus(id, newStatus) {
    return apiPatch(`/api/formcases/${id}/status`, { newStatus });
}
