import { apiGet } from "./client";

export async function getDepartments() {
    return apiGet("/api/departments");
}
