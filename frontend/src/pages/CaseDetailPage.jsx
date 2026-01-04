import React from "react";
import { useNavigate, useParams } from "react-router-dom";

import Typography from "@mui/material/Typography";
import Paper from "@mui/material/Paper";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";
import TextField from "@mui/material/TextField";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";

import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";

import { getCaseById, updateCaseStatus } from "../api/formCaseApi";
import { getAttachments, openAttachmentInNewTab } from "../api/attachmentApi";
import { getClarifications, addClarification } from "../api/clarificationApi";

const TOKEN_KEY = "caseflow_token";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5180";

function decodeJwtPayload(token) {
    try {
        const payload = token.split(".")[1];
        const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(json);
    } catch {
        return null;
    }
}

function normalizeToStringArray(value) {
    if (value == null) return [];
    if (Array.isArray(value)) return value.map(String).filter(Boolean);
    return [String(value)].filter(Boolean);
}

function getRolesFromToken() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return [];

    const payload = decodeJwtPayload(token);
    if (!payload) return [];

    const roleUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    const roles = [
        ...normalizeToStringArray(payload.role),
        ...normalizeToStringArray(payload.roles),
        ...normalizeToStringArray(payload[roleUri]),
    ];

    return Array.from(new Set(roles));
}

function getEmployeeIdFromToken() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;

    const payload = decodeJwtPayload(token);
    if (!payload) return null;

    const raw =
        payload.employeeId ??
        payload.EmployeeId ??
        payload["employeeId"] ??
        payload["EmployeeId"] ??
        payload["EmployeeID"] ??
        null;

    if (raw == null) return null;
    const n = Number(raw);
    return Number.isFinite(n) ? n : null;
}

function getOwnerEmployeeIdFromCase(caseData) {
    if (!caseData) return null;

    const raw =
        caseData.createByEmployeeId ??
        caseData.createdByEmployeeId ??
        caseData.createByEmployeeID ??
        caseData.createdByEmployeeID ??
        caseData.createdByEmployee?.id ??
        caseData.createByEmployee?.id ??
        caseData.createByEmployee?.employeeId ??
        null;

    if (raw == null) return null;
    const n = Number(raw);
    return Number.isFinite(n) ? n : null;
}

async function deleteFormCase(id) {
    const token = localStorage.getItem(TOKEN_KEY);

    const res = await fetch(`${API_BASE_URL}/api/formcases/${id}`, {
        method: "DELETE",
        headers: {
            Authorization: token ? `Bearer ${token}` : undefined,
        },
    });

    if (!res.ok) {
        let message = "Delete failed";
        try {
            const body = await res.json();
            message = body?.error ?? body?.message ?? JSON.stringify(body);
        } catch {
            try {
                const text = await res.text();
                if (text) message = text;
            } catch {
                /* ignore */
            }
        }
        throw new Error(message);
    }

    return null;
}

export default function CaseDetailPage() {
    const { id } = useParams();
    const navigate = useNavigate();

    const [caseData, setCaseData] = React.useState(null);
    const [attachments, setAttachments] = React.useState([]);
    const [clarifications, setClarifications] = React.useState([]);

    const [busy, setBusy] = React.useState(false);
    const [error, setError] = React.useState(null);

    const [editMode, setEditMode] = React.useState(false);

    const [clarifyOpen, setClarifyOpen] = React.useState(false);
    const [clarifyText, setClarifyText] = React.useState("");

    const roles = getRolesFromToken();
    const employeeId = getEmployeeIdFromToken();

    const isSachbearbeiter = roles.includes("Sachbearbeiter");
    const isErfasser = roles.includes("Erfasser");

    const loadAll = React.useCallback(async () => {
        setError(null);
        try {
            const c = await getCaseById(id);
            setCaseData(c);

            const a = await getAttachments(id);
            setAttachments(Array.isArray(a) ? a : []);

            const cl = await getClarifications(id);
            setClarifications(Array.isArray(cl) ? cl : []);
        } catch (e) {
            setError(e?.message ?? "Fehler beim Laden");
        }
    }, [id]);

    React.useEffect(() => {
        loadAll();
    }, [loadAll]);

    // ✅ Status kommt bei dir als STRING: "Neu" | "InBearbeitung" | "InKlaerung" | "Erledigt"
    const status = caseData?.status;

    const ownerEmployeeId = getOwnerEmployeeIdFromCase(caseData);

    const isOwner =
        employeeId != null &&
        ownerEmployeeId != null &&
        Number(employeeId) === Number(ownerEmployeeId);

    // Sachbearbeiter: Bearbeiten sichtbar bei Neu oder InBearbeitung
    const canSachbearbeiterStart =
        isSachbearbeiter && (status === "Neu" || status === "InBearbeitung");

    // Sachbearbeiter Aktionen nur bei InBearbeitung
    const canSachbearbeiterActions =
        isSachbearbeiter && status === "InBearbeitung";

    // Erfasser: nur bei InKlaerung UND nur Owner
    const canErfasserStart =
        isErfasser && status === "InKlaerung" && isOwner;

    const setStatus = async (newStatus) => {
        setBusy(true);
        setError(null);
        try {
            await updateCaseStatus(id, newStatus); // ✅ STRING
            await loadAll();
        } catch (e) {
            setError(e?.message ?? "Statusänderung fehlgeschlagen");
        } finally {
            setBusy(false);
        }
    };

    const onStartEdit = async () => {
        // Sachbearbeiter: Neu -> InBearbeitung, dann Edit Mode
        if (isSachbearbeiter && status === "Neu") {
            await setStatus("InBearbeitung");
        }
        setEditMode(true);
    };

    const onConfirmInKlaerung = async () => {
        const msg = clarifyText.trim();
        if (!msg) return;

        setBusy(true);
        setError(null);

        try {
            // 1) Clarification anlegen (Sachbearbeiter)
            await addClarification(id, msg);

            // 2) Status setzen
            await updateCaseStatus(id, "InKlaerung");

            setClarifyText("");
            setClarifyOpen(false);
            setEditMode(false);

            await loadAll();
        } catch (e) {
            setError(e?.message ?? "In Klärung fehlgeschlagen");
        } finally {
            setBusy(false);
        }
    };

    const onDelete = async () => {
        setBusy(true);
        setError(null);
        try {
            await deleteFormCase(id);
            navigate("/", { replace: true });
        } catch (e) {
            setError(e?.message ?? "Löschen fehlgeschlagen");
        } finally {
            setBusy(false);
        }
    };

    if (!caseData) {
        return (
            <Stack spacing={2}>
                {error && <Alert severity="error">{error}</Alert>}
                <Typography>Lade Fall...</Typography>
            </Stack>
        );
    }

    return (
        <Stack spacing={2}>
            <Typography variant="h5">Fall #{caseData.id}</Typography>

            {error && <Alert severity="error">{error}</Alert>}

            <Paper sx={{ p: 2 }}>
                <Stack
                    direction="row"
                    spacing={2}
                    alignItems="center"
                    justifyContent="space-between"
                >
                    <Typography>
                        Status: <b>{String(status)}</b>
                    </Typography>

                    <Stack direction="row" spacing={1}>
                        {!editMode && (canSachbearbeiterStart || canErfasserStart) && (
                            <Button variant="outlined" disabled={busy} onClick={onStartEdit}>
                                Bearbeiten
                            </Button>
                        )}

                        {editMode && canSachbearbeiterActions && (
                            <>
                                <Button
                                    disabled={busy}
                                    variant="outlined"
                                    onClick={() => setClarifyOpen(true)}
                                >
                                    In Klärung
                                </Button>

                                <Button
                                    disabled={busy}
                                    variant="contained"
                                    onClick={async () => {
                                        await setStatus("Erledigt");
                                        setEditMode(false);
                                    }}
                                >
                                    Abschließen
                                </Button>
                            </>
                        )}

                        {editMode && canErfasserStart && (
                            <>
                                <Button disabled={busy} variant="outlined" onClick={onDelete}>
                                    Löschen
                                </Button>

                                <Button
                                    disabled={busy}
                                    variant="contained"
                                    onClick={async () => {
                                        await setStatus("Neu");
                                        setEditMode(false);
                                    }}
                                >
                                    Erneut senden
                                </Button>
                            </>
                        )}

                        {editMode && (
                            <Button
                                variant="text"
                                disabled={busy}
                                onClick={() => setEditMode(false)}
                            >
                                Abbrechen
                            </Button>
                        )}

                        <Button disabled={busy} variant="text" onClick={() => window.close()}>
                            Schließen
                        </Button>
                    </Stack>
                </Stack>

                {/* Debug-Info (kannst du später entfernen) */}
                <Typography variant="caption" color="text.secondary">
                    roles={JSON.stringify(roles)} employeeId={String(employeeId)} ownerEmployeeId={String(ownerEmployeeId)} isOwner={String(isOwner)} status={String(status)}
                </Typography>
            </Paper>

            <Dialog
                open={clarifyOpen}
                onClose={() => (busy ? null : setClarifyOpen(false))}
                fullWidth
                maxWidth="sm"
            >
                <DialogTitle>In Klärung setzen</DialogTitle>
                <DialogContent>
                    <TextField
                        label="Klärungsnachricht (Pflicht)"
                        value={clarifyText}
                        onChange={(e) => setClarifyText(e.target.value)}
                        multiline
                        minRows={3}
                        fullWidth
                        sx={{ mt: 1 }}
                        disabled={busy}
                    />
                </DialogContent>
                <DialogActions>
                    <Button disabled={busy} onClick={() => setClarifyOpen(false)}>
                        Abbrechen
                    </Button>
                    <Button
                        variant="contained"
                        disabled={busy || clarifyText.trim().length === 0}
                        onClick={onConfirmInKlaerung}
                    >
                        Senden & In Klärung
                    </Button>
                </DialogActions>
            </Dialog>

            <Paper sx={{ p: 2 }}>
                <Typography variant="h6">Antragsteller</Typography>
                <Divider sx={{ my: 1 }} />
                <Typography>Name: {caseData.applicantName}</Typography>
                <Typography>E-Mail: {caseData.applicantEmail}</Typography>
                <Typography>Straße: {caseData.applicantStreet}</Typography>
                <Typography>PLZ: {caseData.applicantZip}</Typography>
                <Typography>Stadt: {caseData.applicantCity}</Typography>
                <Typography>Telefon: {caseData.applicantPhone}</Typography>

                <Divider sx={{ my: 2 }} />

                <Typography variant="h6">Fallinformationen</Typography>
                <Divider sx={{ my: 1 }} />
                <Typography>Betreff: {caseData.subject ?? "-"}</Typography>
                <Typography>Notizen: {caseData.notes ?? "-"}</Typography>
            </Paper>

            <Paper sx={{ p: 2 }}>
                <Typography variant="h6">Anhänge (PDF)</Typography>
                <Divider sx={{ my: 1 }} />
                <List>
                    {attachments.map((a) => (
                        <ListItem key={a.id} disablePadding>
                            <Button
                                onClick={() => openAttachmentInNewTab(a.id)}
                                sx={{ justifyContent: "flex-start", width: "100%" }}
                            >
                                {a.fileName ?? `Attachment ${a.id}`}
                            </Button>
                        </ListItem>
                    ))}

                    {attachments.length === 0 && (
                        <Typography variant="body2" color="text.secondary">
                            Keine Anhänge vorhanden
                        </Typography>
                    )}
                </List>
            </Paper>

            <Paper sx={{ p: 2 }}>
                <Typography variant="h6">Klärungsnachrichten</Typography>
                <Divider sx={{ my: 1 }} />

                <List>
                    {clarifications.map((c) => (
                        <ListItem key={c.id} alignItems="flex-start">
                            <ListItemText
                                primary={c.message}
                                secondary={`Erstellt am ${new Date(c.createdAt).toLocaleString()} • Mitarbeiter #${c.createdByEmployeeId}`}
                            />
                        </ListItem>
                    ))}

                    {clarifications.length === 0 && (
                        <Typography variant="body2" color="text.secondary">
                            Noch keine Klärungsnachrichten.
                        </Typography>
                    )}
                </List>

                <Typography variant="body2" color="text.secondary">
                    Klärungsnachrichten sind nur zur Information und können nicht beantwortet werden.
                </Typography>
            </Paper>
        </Stack>
    );
}
