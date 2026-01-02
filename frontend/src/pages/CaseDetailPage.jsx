import React from "react";
import { useParams } from "react-router-dom";
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

import { getCaseById, updateCaseStatus } from "../api/formCaseApi";
import { getAttachments, openAttachmentInNewTab } from "../api/attachmentApi";
import { getClarifications, addClarification } from "../api/clarificationApi";

function decodeJwtPayload(token) {
    try {
        const payload = token.split(".")[1];
        const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(json);
    } catch {
        return null;
    }
}

function getRoleFromToken() {
    const token = localStorage.getItem("caseflow_token");
    if (!token) return null;
    const payload = decodeJwtPayload(token);
    // je nachdem wie du claims setzt – häufig "role" oder standard claim
    return payload?.role ?? payload?.["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? null;
}

export default function CaseDetailPage() {
    const { id } = useParams();

    const [caseData, setCaseData] = React.useState(null);
    const [attachments, setAttachments] = React.useState([]);
    const [clarifications, setClarifications] = React.useState([]);

    const [busy, setBusy] = React.useState(false);
    const [error, setError] = React.useState(null);

    const [msg, setMsg] = React.useState("");
    const role = getRoleFromToken();

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

    const status = caseData?.status; // z.B. "Neu" | "InBearbeitung" | "InKlaerung" | "Erledigt"

    const canWriteClarification =
        status === "InKlaerung" &&
        role !== "Stammdaten"; // minimal: Stammdaten nur lesen

    const onSendClarification = async () => {
        const trimmed = msg.trim();
        if (!trimmed) return;

        setBusy(true);
        setError(null);
        try {
            await addClarification(id, trimmed);
            setMsg("");
            await loadAll();
        } catch (e) {
            setError(e?.message ?? "Fehler beim Senden");
        } finally {
            setBusy(false);
        }
    };

    const setStatus = async (newStatus) => {
        setBusy(true);
        setError(null);
        try {
            await updateCaseStatus(id, newStatus);
            await loadAll();
        } catch (e) {
            setError(e?.message ?? "Statusänderung fehlgeschlagen");
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

            {/* Status/Actions */}
            <Paper sx={{ p: 2 }}>
                <Stack direction="row" spacing={2} alignItems="center" justifyContent="space-between">
                    <Typography>
                        Status: <b>{status}</b>
                    </Typography>

                    <Stack direction="row" spacing={1}>
                        {/* Minimal: Buttons nur wenn du sie schon vorher hattest.
                Deine eigentliche Bearbeiten-Logik (Edit Mode) bleibt unverändert.
                Hier nur die Status-Buttons: */}
                        {status === "InBearbeitung" && (
                            <>
                                <Button disabled={busy} variant="outlined" onClick={() => setStatus("InKlaerung")}>
                                    In Klärung
                                </Button>
                                <Button disabled={busy} variant="contained" onClick={() => setStatus("Erledigt")}>
                                    Abschließen
                                </Button>
                            </>
                        )}
                        <Button disabled={busy} variant="text" onClick={() => window.close()}>
                            Schließen
                        </Button>
                    </Stack>
                </Stack>
            </Paper>

            {/* Form Details (du hast das Layout schon – hier minimal) */}
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

            {/* Attachments */}
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

            {/* Clarifications */}
            <Paper sx={{ p: 2 }}>
                <Typography variant="h6">Klärungsnachrichten</Typography>
                <Divider sx={{ my: 1 }} />

                <List>
                    {clarifications.map((c) => (
                        <ListItem key={c.id} alignItems="flex-start">
                            <ListItemText
                                primary={c.message}
                                secondary={
                                    `Erstellt am ${new Date(c.createdAt).toLocaleString()} • Mitarbeiter #${c.createdByEmployeeId}`
                                }
                            />
                        </ListItem>
                    ))}

                    {clarifications.length === 0 && (
                        <Typography variant="body2" color="text.secondary">
                            Noch keine Klärungsnachrichten.
                        </Typography>
                    )}
                </List>


                {canWriteClarification && (
                    <>
                        <Divider sx={{ my: 2 }} />
                        <Stack spacing={1}>
                            <TextField
                                label="Nachricht"
                                value={msg}
                                onChange={(e) => setMsg(e.target.value)}
                                multiline
                                minRows={3}
                            />
                            <Stack direction="row" spacing={1} justifyContent="flex-end">
                                <Button
                                    variant="contained"
                                    disabled={busy || msg.trim().length === 0}
                                    onClick={onSendClarification}
                                >
                                    Senden
                                </Button>
                            </Stack>
                        </Stack>
                    </>
                )}

                {!canWriteClarification && status !== "InKlaerung" && (
                    <Typography variant="body2" color="text.secondary">
                        Nachrichten können nur im Status <b>In Klärung</b> hinzugefügt werden.
                    </Typography>
                )}
            </Paper>
        </Stack>
    );
}
