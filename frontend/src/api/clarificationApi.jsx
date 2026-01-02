import { apiGet, apiPost } from "./client";

export async function getClarifications(formCaseId) {
    return apiGet(`/api/formcases/${formCaseId}/clarifications`);
}

export async function addClarification(formCaseId, message) {
    return apiPost(`/api/formcases/${formCaseId}/clarifications`, { message });
}
