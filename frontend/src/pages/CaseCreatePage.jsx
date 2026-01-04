import { useNavigate } from "react-router-dom";
import { createCase } from "../api/formCaseApi";
import React, { useEffect, useState } from "react";

import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Typography from "@mui/material/Typography";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";
import MenuItem from "@mui/material/MenuItem";
import Divider from "@mui/material/Divider";
import InputAdornment from "@mui/material/InputAdornment";
import { getDepartments } from "../api/departmentApi";
import { uploadAttachment } from "../api/attachmentApi";


const FORM_TYPES = [
    { value: 0, label: "Leistungsantrag" },
    { value: 1, label: "Kostenantrag" },
    { value: 2, label: "Organisationsantrag" },
];

function asInt(v) {
    const n = Number(v);
    return Number.isFinite(n) ? n : 0;
}

function buildPayload(state) {
    // Nur relevante Felder senden: Basis + typ-spezifische
    const base = {
        formType: asInt(state.formType),
        departmentId: asInt(state.departmentId),

        applicantName: state.applicantName.trim(),
        applicantStreet: state.applicantStreet.trim(),
        applicantZip: asInt(state.applicantZip),
        applicantCity: state.applicantCity.trim(),
        applicantPhone: state.applicantPhone.trim(),
        applicantEmail: state.applicantEmail.trim(),

        subject: state.subject.trim() || null,
        notes: state.notes.trim() || null,
    };

    if (base.formType === 0) {
        return {
            ...base,
            serviceDescription: state.serviceDescription.trim() || null,
            justification: state.justification.trim() || null,
            amount: null,
            costType: null,
            changeRequest: null,
        };
    }

    if (base.formType === 1) {
        return {
            ...base,
            amount: state.amount === "" ? null : Number(state.amount),
            costType: state.costType.trim() || null,
            serviceDescription: null,
            justification: null,
            changeRequest: null,
        };
    }

    return {
        ...base,
        changeRequest: state.changeRequest.trim() || null,
        serviceDescription: null,
        justification: null,
        amount: null,
        costType: null,
    };
}

function validate(state, pdfFile) {
    const errors = {};

    // Basis
    if (!state.applicantName.trim()) errors.applicantName = "Pflichtfeld";
    if (!state.applicantStreet.trim()) errors.applicantStreet = "Pflichtfeld";
    if (!String(state.applicantZip).trim()) errors.applicantZip = "Pflichtfeld";
    if (!state.applicantCity.trim()) errors.applicantCity = "Pflichtfeld";
    if (!state.applicantEmail.trim()) errors.applicantEmail = "Pflichtfeld";
    if (!String(state.departmentId).trim()) errors.departmentId = "Pflichtfeld";

    // PDF Pflicht
    if (!pdfFile) errors.pdfFile = "PDF ist erforderlich";

    // Typ-spezifisch (wie bei dir)
    const ft = asInt(state.formType);
    if (ft === 0) {
        if (!state.serviceDescription.trim()) errors.serviceDescription = "Pflichtfeld";
        if (!state.justification.trim()) errors.justification = "Pflichtfeld";
    }
    if (ft === 1) {
        if (state.amount === "" || Number(state.amount) <= 0) errors.amount = "Betrag > 0 erforderlich";
        if (!state.costType.trim()) errors.costType = "Pflichtfeld";
    }
    if (ft === 2) {
        if (!state.changeRequest.trim()) errors.changeRequest = "Pflichtfeld";
    }

    return errors;
}


export default function CaseCreatePage() {
    const navigate = useNavigate();

    const [state, setState] = React.useState({
        formType: 0,

        departmentId: "",
        applicantName: "",
        applicantStreet: "",
        applicantZip: "",
        applicantCity: "",
        applicantPhone: "",
        applicantEmail: "",
        subject: "",
        notes: "",

        serviceDescription: "",
        justification: "",

        amount: "",
        costType: "",

        changeRequest: "",
    });

    const [errors, setErrors] = React.useState({});
    const [submitError, setSubmitError] = React.useState(null);
    const [submitting, setSubmitting] = React.useState(false);
    const [departments, setDepartments] = useState([]);
    const [deptLoading, setDeptLoading] = React.useState(false);
    const [pdfFile, setPdfFile] = React.useState(null);

    function setField(name, value) {
        setState((prev) => ({ ...prev, [name]: value }));
    }
    useEffect(() => {
        let mounted = true;

        (async () => {
            try {
                setDeptLoading(true);
                const data = await getDepartments();
                if (mounted) setDepartments(Array.isArray(data) ? data : []);
            } catch (e) {
                console.error("Load departments failed:", e);
            } finally {
                if (mounted) setDeptLoading(false);
            }
        })();

        return () => { mounted = false; };
    }, []);

    async function onSubmit(e) {
        e.preventDefault();
        setSubmitError(null);

        const v = validate(state, pdfFile);
        setErrors(v);
        if (Object.keys(v).length > 0) return;

        setSubmitting(true);
        try {
            const payload = buildPayload(state);
            const created = await createCase(payload);

            await uploadAttachment(created.id, pdfFile);


            // Falls API das neue Objekt inkl. id zurückgibt:
            if (created?.id) {
                navigate(`/cases/${created.id}`);
            } else {
                navigate("/cases");
            }
        } catch (err) {
            setSubmitError(err?.message || "Fehler beim Anlegen.");
        } finally {
            setSubmitting(false);
        }
    }

    const formType = asInt(state.formType);

    return (
        <Box>
            <Typography variant="h5" fontWeight={600} sx={{ mb: 2 }}>
                Neuer Fall
            </Typography>

            <Paper variant="outlined" sx={{ p: 2 }}>
                <form onSubmit={onSubmit}>
                    <Stack spacing={2}>
                        {submitError && <Alert severity="error">{submitError}</Alert>}

                        <TextField
                            select
                            label="Antragstyp"
                            value={state.formType}
                            onChange={(e) => setField("formType", e.target.value)}
                            fullWidth
                        >
                            {FORM_TYPES.map((t) => (
                                <MenuItem key={t.value} value={t.value}>
                                    {t.label}
                                </MenuItem>
                            ))}
                        </TextField>

                        <Divider />

                        <Typography variant="subtitle1" fontWeight={600}>
                            Antragsteller
                        </Typography>

                        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                            <TextField
                                label="Name"
                                value={state.applicantName}
                                onChange={(e) => setField("applicantName", e.target.value)}
                                error={!!errors.applicantName}
                                helperText={errors.applicantName}
                                fullWidth
                            />
                            <TextField
                                label="E-Mail"
                                type="email"
                                value={state.applicantEmail}
                                onChange={(e) => setField("applicantEmail", e.target.value)}
                                error={!!errors.applicantEmail}
                                helperText={errors.applicantEmail}
                                fullWidth
                            />
                        </Stack>

                        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                            <TextField
                                label="Straße"
                                value={state.applicantStreet}
                                onChange={(e) => setField("applicantStreet", e.target.value)}
                                error={!!errors.applicantStreet}
                                helperText={errors.applicantStreet}
                                fullWidth
                            />
                            <TextField
                                label="PLZ"
                                type="number"
                                value={state.applicantZip}
                                onChange={(e) => setField("applicantZip", e.target.value)}
                                error={!!errors.applicantZip}
                                helperText={errors.applicantZip}
                                fullWidth
                            />
                            <TextField
                                label="Stadt"
                                value={state.applicantCity}
                                onChange={(e) => setField("applicantCity", e.target.value)}
                                error={!!errors.applicantCity}
                                helperText={errors.applicantCity}
                                fullWidth
                            />
                        </Stack>

                        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                            <TextField
                                label="Telefon"
                                value={state.applicantPhone}
                                onChange={(e) => setField("applicantPhone", e.target.value)}
                                fullWidth
                            />
                            <TextField
                                select
                                label="Abteilung"
                                value={state.departmentId}
                                onChange={(e) => setField("departmentId", e.target.value)}
                                error={!!errors.departmentId}
                                helperText={errors.departmentId}
                                fullWidth
                                disabled={deptLoading}
                            >
                                <MenuItem value="">
                                    <em>Bitte auswählen</em>
                                </MenuItem>

                                {departments.map((d) => (
                                    <MenuItem key={d.id} value={String(d.id)}>
                                        {d.name}
                                    </MenuItem>
                                ))}
                            </TextField>

                        </Stack>

                        <Divider />

                        <Typography variant="subtitle1" fontWeight={600}>
                            Fallinformationen
                        </Typography>

                        <TextField
                            label="Betreff"
                            value={state.subject}
                            onChange={(e) => setField("subject", e.target.value)}
                            fullWidth
                        />

                        <TextField
                            label="Notizen"
                            value={state.notes}
                            onChange={(e) => setField("notes", e.target.value)}
                            multiline
                            minRows={3}
                            fullWidth
                        />

                        <Divider />

                        {formType === 0 && (
                            <>
                                <Typography variant="subtitle1" fontWeight={600}>
                                    Leistungsantrag
                                </Typography>

                                <TextField
                                    label="Leistungsbeschreibung"
                                    value={state.serviceDescription}
                                    onChange={(e) => setField("serviceDescription", e.target.value)}
                                    error={!!errors.serviceDescription}
                                    helperText={errors.serviceDescription}
                                    multiline
                                    minRows={3}
                                    fullWidth
                                />

                                <TextField
                                    label="Begründung"
                                    value={state.justification}
                                    onChange={(e) => setField("justification", e.target.value)}
                                    error={!!errors.justification}
                                    helperText={errors.justification}
                                    multiline
                                    minRows={3}
                                    fullWidth
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
                                        value={state.amount}
                                        onChange={(e) => setField("amount", e.target.value)}
                                        error={!!errors.amount}
                                        helperText={errors.amount}
                                        fullWidth
                                        InputProps={{
                                            startAdornment: <InputAdornment position="start">€</InputAdornment>,
                                        }}
                                    />
                                    <TextField
                                        label="Kostenart"
                                        value={state.costType}
                                        onChange={(e) => setField("costType", e.target.value)}
                                        error={!!errors.costType}
                                        helperText={errors.costType}
                                        fullWidth
                                    />
                                </Stack>
                            </>
                        )}

                        {formType === 2 && (
                            <>
                                <Typography variant="subtitle1" fontWeight={600}>
                                    Organisationsantrag
                                </Typography>

                                <TextField
                                    label="Änderungsantrag"
                                    value={state.changeRequest}
                                    onChange={(e) => setField("changeRequest", e.target.value)}
                                    error={!!errors.changeRequest}
                                    helperText={errors.changeRequest}
                                    multiline
                                    minRows={4}
                                    fullWidth
                                />

                            </>
                        )}

                        <Typography variant="subtitle1" fontWeight={600}>
                            Pflicht-Anhang (PDF)
                        </Typography>

                        <TextField
                            type="file"
                            inputProps={{ accept: "application/pdf" }}
                            error={!!errors.pdfFile}
                            helperText={errors.pdfFile}
                            onChange={(e) => setPdfFile(e.target.files?.[0] ?? null)}
                            fullWidth
                        />


                        <Divider />

                        <Stack direction="row" spacing={1} justifyContent="flex-end">
                            <Button variant="outlined" onClick={() => navigate("/cases")} disabled={submitting}>
                                Abbrechen
                            </Button>
                            <Button type="submit" variant="contained" disabled={submitting}>
                                {submitting ? "Speichern…" : "Speichern"}
                            </Button>
                        </Stack>
                    </Stack>
                </form>
            </Paper>
        </Box>
    );
}