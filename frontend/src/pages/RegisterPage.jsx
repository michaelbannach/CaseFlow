// src/pages/RegisterPage.jsx
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { register } from "../api/authApi";
import { getDepartments } from "../api/departmentApi";

import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";

export default function RegisterPage() {
    const navigate = useNavigate();

    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const [role, setRole] = useState("Erfasser");
    const [departmentId, setDepartmentId] = useState(""); // string for Select, convert later

    const [departments, setDepartments] = useState([]);
    const [error, setError] = useState("");
    const [info, setInfo] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    const roleOptions = useMemo(() => ["Erfasser", "Sachbearbeiter", "Stammdaten"], []);

    useEffect(() => {
        let mounted = true;
        (async () => {
            try {
                const data = await getDepartments();
                if (!mounted) return;
                setDepartments(data ?? []);
            } catch (err) {
                if (!mounted) return;
                setError(err?.message ?? "Departments konnten nicht geladen werden.");
            }
        })();
        return () => {
            mounted = false;
        };
    }, []);

    async function onSubmit(e) {
        e.preventDefault();
        setError("");
        setInfo("");
        setIsSubmitting(true);

        try {
            // DepartmentId ist im Backend int?; leeres Select => null
            const dept = departmentId === "" ? null : Number(departmentId);

            await register({
                name,
                email,
                password,
                role,
                departmentId: dept,
            });

            setInfo("Registrierung erfolgreich. Bitte einloggen.");
            navigate("/login", { replace: true });
        } catch (err) {
            setError(err?.message ?? "Registrierung fehlgeschlagen");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center", p: 2 }}>
            <Paper sx={{ width: 520, p: 3 }}>
                <Typography variant="h5" sx={{ mb: 2 }}>
                    Registrierung
                </Typography>

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}
                {info && (
                    <Alert severity="success" sx={{ mb: 2 }}>
                        {info}
                    </Alert>
                )}

                <Box component="form" onSubmit={onSubmit} sx={{ display: "grid", gap: 2 }}>
                    <TextField
                        label="Name"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                    />

                    <TextField
                        label="E-Mail"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        autoComplete="email"
                    />

                    <TextField
                        label="Passwort"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        autoComplete="new-password"
                        helperText="Mindestens so wie im Backend (z. B. Test123! …)."
                    />

                    <FormControl>
                        <InputLabel id="role-label">Rolle</InputLabel>
                        <Select
                            labelId="role-label"
                            label="Rolle"
                            value={role}
                            onChange={(e) => setRole(e.target.value)}
                        >
                            {roleOptions.map((r) => (
                                <MenuItem key={r} value={r}>
                                    {r}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>

                    <FormControl>
                        <InputLabel id="dept-label">Abteilung</InputLabel>
                        <Select
                            labelId="dept-label"
                            label="Abteilung"
                            value={departmentId}
                            onChange={(e) => setDepartmentId(e.target.value)}
                        >
                            <MenuItem value="">
                                <em>Keine (null)</em>
                            </MenuItem>

                            {departments.map((d) => (
                                <MenuItem key={d.id} value={String(d.id)}>
                                    {d.name ?? `Department ${d.id}`}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>

                    <Button type="submit" variant="contained" disabled={isSubmitting}>
                        {isSubmitting ? "Bitte warten…" : "Registrieren"}
                    </Button>

                    <Button variant="text" onClick={() => navigate("/login")}>
                        Zurück zum Login
                    </Button>
                </Box>
            </Paper>
        </Box>
    );
}
