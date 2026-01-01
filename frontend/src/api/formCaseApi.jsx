// src/api/formCaseApi.jsx
import { apiPost, apiGet, apiPatch } from "./client";

// Create
export function createCase(payload) {
    return apiPost("/api/formcases", payload);
}

// List all cases
export function getCases() {
    return apiGet("/api/formcases");
}

// Get single case
export function getCaseById(id) {
    return apiGet(`/api/formcases/${id}`);
}

// Update status
export function updateCaseStatus(id, newStatus) {
    return apiPatch(`/api/formcases/${id}/status`, { newStatus });
}
