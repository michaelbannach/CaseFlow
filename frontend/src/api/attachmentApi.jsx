import { apiGet, apiPost, getToken } from "./client";

const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5180";


export function getAttachments(formCaseId) {
    return apiGet(`/api/formcases/${formCaseId}/attachments`);
}


export function uploadAttachment(formCaseId, file) {
    const form = new FormData();
    form.append("File", file, file.name);

    return apiPost(`/api/formcases/${formCaseId}/attachments`, form);
}

export async function openAttachmentInNewTab(attachmentId) {
    const token = getToken();

    const res = await fetch(
        `${API_BASE_URL}/api/attachments/${attachmentId}/download`,
        {
            method: "GET",
            headers: {
                Authorization: `Bearer ${token}`,
            },
        }
    );

    if (!res.ok) {
        const text = await res.text();
        throw new Error(text || "PDF konnte nicht geladen werden");
    }

    const blob = await res.blob();
    const url = URL.createObjectURL(blob);

    // Open in new tab
    window.open(url, "_blank", "noopener,noreferrer");

    // Cleanup
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
