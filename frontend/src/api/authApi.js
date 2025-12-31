// src/api/authApi.js
import { apiPost } from "./client";

export function login(email, password) {
    return apiPost("/api/auth/login", { email, password });
}

export function register(payload) {
    // payload: { email, password, name, role, departmentId }
    return apiPost("/api/auth/register", payload);
}
