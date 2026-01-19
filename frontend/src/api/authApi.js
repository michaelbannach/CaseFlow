import { apiPost } from "./client";

export function login(email, password) {
    return apiPost("/api/auth/login", { email, password });
}

export function register(payload) {
    
    return apiPost("/api/auth/register", payload);
}
