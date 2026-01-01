import React from "react";
import { useParams } from "react-router-dom";

import { getCaseById, updateCaseStatus } from "../api/formCaseApi";
import { getAttachments, openAttachmentInNewTab } from "../api/attachmentApi";
import { getDepartments } from "../api/departmentApi";

import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Typography from "@mui/material/Typography";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Alert from "@mui/material/Alert";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Button from "@mui/material/Button";


const FORM_TYPES = [
    { value: 0, label: "Dienstleistungsantrag" },
    { value: 1, label: "Kostenantrag" },
    { value: 2, label: "Änderungsantrag" },
];

function asInt(v) {
    const n = Number(v);
    return Number.isFinite(n) ? n : 0;
}

export default function CaseDetailPage() {
    const { id } = useParams();

    const [formCase, setFormCase] = React.useState(null);
    const [attachments, setAttachments] = React.useState([]);
    const [departments, setDepartments] = React.useState([]);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState(null);

    const [actionError, setActionError] = React.useState("");
    const [busy, setBusy] = React.useState(false);

    async function setStatus(newStatus) {
        setBusy(true);
        setActionError("");
        try {
            await updateCaseStatus(formCase.id, newStatus);
            await loadAll(); // reload case + attachments + departments
        } catch (e) {
            setActionError(e?.message ?? "Aktion fehlgeschlagen");
        } finally {
            setBusy(false);
        }
    }

    function closeTabOrBack() {
        // funktioniert, weil der Tab per window.open() geöffnet wurde
        window.close();

        // Fallback, falls Browser close blockt
        setTimeout(() => {
            window.location.href = "/cases";
        }, 50);
    }

    React.useEffect(() => {
        let mounted = true;

        (async () => {
            setLoading(true);
            setError(null);

            try {
                const [fc, atts, depts] = await Promise.all([
                    getCaseById(id),
                    getAttachments(id),
                    getDepartments(),
                ]);

                if (!mounted) return;

                setFormCase(fc);
                setAttachments(Array.isArray(atts) ? atts : []);
                setDepartments(Array.isArray(depts) ? depts : []);
            } catch (e) {
                if (!mounted) return;
                setError(e?.message ?? "Fehler beim Laden.");
            } finally {
                if (mounted) setLoading(false);
            }
        })();

        return () => {
            mounted = false;
        };
    }, [id]);

    const deptName =
        formCase?.departmentId && departments.length
            ? (departments.find((d) => d.id === formCase.departmentId)?.name ??
                `Department ${formCase.departmentId}`)
            : "";

    const formType = asInt(formCase?.formType ?? 0);
    const formTypeLabel = FORM_TYPES.find((t) => t.value === formType)?.label ?? String(formType);

    if (loading) {
        return (
            <Typography variant="body1">Lade…</Typography>
        );
    }

    if (error) {
        return <Alert severity="error">{error}</Alert>;
    }

    if (!formCase) {
        return <Alert severity="warning">Fall nicht gefunden.</Alert>;
    }

    return (
        <Box>
            <Typography variant="h5" fontWeight={600} sx={{ mb: 2 }}>
                Fall #{formCase.id}
            </Typography>
            <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
                {formCase.status === "Neu" && (
                    <Button
                        variant="contained"
                        disabled={busy}
                        onClick={() => setStatus("InBearbeitung")}
                    >
                        Bearbeiten
                    </Button>
                )}

                {formCase.status === "InBearbeitung" && (
                    <>
                        <Button
                            variant="outlined"
                            disabled={busy}
                            onClick={() => setStatus("InKlaerung")}
                        >
                            In Klärung
                        </Button>

                        <Button
                            variant="contained"
                            disabled={busy}
                            onClick={() => setStatus("Erledigt")}
                        >
                            Abschließen
                        </Button>

                        <Button variant="text" onClick={closeTabOrBack}>
                            Schließen
                        </Button>
                    </>
                )}
            </Stack>

            {actionError && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {actionError}
                </Alert>
            )}

            <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
                <Stack spacing={2}>
                    <TextField label="Antragstyp" value={formTypeLabel} fullWidth InputProps={{ readOnly: true }} />

                    <Divider />

                    <Typography variant="subtitle1" fontWeight={600}>
                        Antragsteller
                    </Typography>

                    <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                        <TextField label="Name" value={formCase.applicantName ?? ""} fullWidth InputProps={{ readOnly: true }} />
                        <TextField label="E-Mail" value={formCase.applicantEmail ?? ""} fullWidth InputProps={{ readOnly: true }} />
                    </Stack>

                    <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                        <TextField label="Straße" value={formCase.applicantStreet ?? ""} fullWidth InputProps={{ readOnly: true }} />
                        <TextField label="PLZ" value={formCase.applicantZip ?? ""} fullWidth InputProps={{ readOnly: true }} />
                        <TextField label="Stadt" value={formCase.applicantCity ?? ""} fullWidth InputProps={{ readOnly: true }} />
                    </Stack>

                    <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                        <TextField label="Telefon" value={formCase.applicantPhone ?? ""} fullWidth InputProps={{ readOnly: true }} />
                        <TextField label="Abteilung" value={deptName} fullWidth InputProps={{ readOnly: true }} />
                    </Stack>

                    <Divider />

                    <Typography variant="subtitle1" fontWeight={600}>
                        Fallinformationen
                    </Typography>

                    <TextField label="Betreff" value={formCase.subject ?? ""} fullWidth InputProps={{ readOnly: true }} />
                    <TextField
                        label="Notizen"
                        value={formCase.notes ?? ""}
                        fullWidth
                        multiline
                        minRows={3}
                        InputProps={{ readOnly: true }}
                    />

                    <Divider />

                    {formType === 0 && (
                        <>
                            <Typography variant="subtitle1" fontWeight={600}>
                                Dienstleistungsantrag
                            </Typography>
                            <TextField
                                label="Leistungsbeschreibung"
                                value={formCase.serviceDescription ?? ""}
                                fullWidth
                                multiline
                                minRows={3}
                                InputProps={{ readOnly: true }}
                            />
                            <TextField
                                label="Begründung"
                                value={formCase.justification ?? ""}
                                fullWidth
                                multiline
                                minRows={2}
                                InputProps={{ readOnly: true }}
                            />
                        </>
                    )}

                    {formType === 1 && (
                        <>
                            <Typography variant="subtitle1" fontWeight={600}>
                                Kostenantrag
                            </Typography>
                            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                                <TextField
                                    label="Betrag"
                                    value={formCase.amount ?? ""}
                                    fullWidth
                                    InputProps={{ readOnly: true }}
                                />
                                <TextField
                                    label="Kostenart"
                                    value={formCase.costType ?? ""}
                                    fullWidth
                                    InputProps={{ readOnly: true }}
                                />
                            </Stack>
                        </>
                    )}

                    {formType === 2 && (
                        <>
                            <Typography variant="subtitle1" fontWeight={600}>
                                Änderungsantrag
                            </Typography>
                            <TextField
                                label="Änderungswunsch"
                                value={formCase.changeRequest ?? ""}
                                fullWidth
                                multiline
                                minRows={3}
                                InputProps={{ readOnly: true }}
                            />
                        </>
                    )}
                </Stack>
            </Paper>

            <Paper variant="outlined" sx={{ p: 2 }}>
                <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
                    Anhänge (PDF)
                </Typography>

                {attachments.length === 0 ? (
                    <Typography variant="body2">Keine Anhänge vorhanden.</Typography>
                ) : (
                    <List>
                        {attachments.map((a) => (
                            <ListItemButton key={a.id} onClick={() => openAttachmentInNewTab(a.id)}>
                                <ListItemText
                                    primary={a.fileName ?? `Attachment ${a.id}`}
                                    secondary={`${a.sizeBytes ?? ""} bytes`}
                                />
                            </ListItemButton>
                        ))}
                    </List>
                )}
            </Paper>
        </Box>
    );
}
