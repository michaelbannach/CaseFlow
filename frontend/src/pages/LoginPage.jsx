import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiPost, setToken } from "../api/client";

import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";

export default function LoginPage() {
    const navigate = useNavigate();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    async function onSubmit(e) {
        e.preventDefault();
        setError("");
        setLoading(true);

        try {
            const data = await apiPost("/api/auth/login", {
                email,
                password,
            });

            // Backend liefert { token: "..." }
            setToken(data.token ?? data.Token);
            navigate("/", { replace: true });
        } catch (err) {
            setError(err.message ?? "Login fehlgeschlagen");
        } finally {
            setLoading(false);
        }
    }

    return (
        <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center" }}>
            <Paper sx={{ width: 420, p: 3 }}>
                <Typography variant="h5" sx={{ mb: 2 }}>
                    Login
                </Typography>

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}

                <Box component="form" onSubmit={onSubmit} sx={{ display: "grid", gap: 2 }}>
                    <TextField
                        label="E-Mail"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />

                    <TextField
                        label="Passwort"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />

                    <Button type="submit" variant="contained" disabled={loading}>
                        {loading ? "Bitte warten…" : "Einloggen"}
                    </Button>

                    <Button variant="text" onClick={() => navigate("/register")}>
                        Registrieren
                    </Button>
                </Box>
            </Paper>
        </Box>
    );
}
