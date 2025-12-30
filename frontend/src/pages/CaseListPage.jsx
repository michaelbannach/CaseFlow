import * as React from "react";
import { useNavigate } from "react-router-dom";

import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Typography from "@mui/material/Typography";
import Toolbar from "@mui/material/Toolbar";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";
import CircularProgress from "@mui/material/CircularProgress";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Stack from "@mui/material/Stack";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";

import RefreshIcon from "@mui/icons-material/Refresh";
import AddIcon from "@mui/icons-material/Add";

import { getCases } from "../api/formCaseApi";
import StatusChip from "../components/StatusChip";

function safeText(v) {
    return (v ?? "").toString().trim();
}

function matchesQuery(c, q) {
    if (!q) return true;
    const query = q.toLowerCase();

    // Passe Felder an, je nachdem was dein Backend liefert:
    // id, applicantName, subject, status, formType, departmentId, createdAt
    const haystack = [
        safeText(c.id),
        safeText(c.applicantName),
        safeText(c.subject),
        safeText(c.status),
        safeText(c.formType),
        safeText(c.departmentId),
    ]
        .join(" ")
        .toLowerCase();

    return haystack.includes(query);
}

export default function CaseListPage() {
    const navigate = useNavigate();

    const [rows, setRows] = React.useState([]);
    const [loading, setLoading] = React.useState(true);
    const [error, setError] = React.useState(null);
    const [query, setQuery] = React.useState("");

    const load = React.useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await getCases();
            setRows(Array.isArray(data) ? data : []);
        } catch (e) {
            setError(e?.message || "Fehler beim Laden der Fälle.");
        } finally {
            setLoading(false);
        }
    }, []);

    React.useEffect(() => {
        load();
    }, [load]);

    const filtered = React.useMemo(() => {
        return rows.filter((c) => matchesQuery(c, query));
    }, [rows, query]);

    return (
        <Box>
            <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={2}
                alignItems={{ xs: "stretch", sm: "center" }}
                justifyContent="space-between"
                sx={{ mb: 2 }}
            >
                <Box>
                    <Typography variant="h5" fontWeight={600}>
                        Fälle
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                        Übersicht aller Formulareingänge
                    </Typography>
                </Box>

                <Stack direction="row" spacing={1} alignItems="center">
                    <Tooltip title="Neu laden">
            <span>
              <IconButton onClick={load} disabled={loading} aria-label="reload">
                <RefreshIcon />
              </IconButton>
            </span>
                    </Tooltip>

                    <Button
                        variant="contained"
                        startIcon={<AddIcon />}
                        onClick={() => navigate("/cases/new")}
                    >
                        Neuer Fall
                    </Button>
                </Stack>
            </Stack>

            <Paper variant="outlined">
                <Toolbar sx={{ gap: 2, flexWrap: "wrap" }}>
                    <TextField
                        size="small"
                        label="Suche"
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        placeholder="z.B. Name, Status, Typ, ID…"
                        sx={{ width: { xs: "100%", sm: 360 } }}
                    />
                    <Typography variant="body2" color="text.secondary">
                        {filtered.length} von {rows.length}
                    </Typography>
                </Toolbar>

                {error && (
                    <Box sx={{ px: 2, pb: 2 }}>
                        <Alert severity="error">{error}</Alert>
                    </Box>
                )}

                {loading ? (
                    <Box sx={{ p: 3, display: "flex", justifyContent: "center" }}>
                        <CircularProgress />
                    </Box>
                ) : (
                    <TableContainer>
                        <Table size="medium">
                            <TableHead>
                                <TableRow>
                                    <TableCell width={90}>ID</TableCell>
                                    <TableCell>Applicant</TableCell>
                                    <TableCell>Betreff</TableCell>
                                    <TableCell width={170}>Typ</TableCell>
                                    <TableCell width={160}>Status</TableCell>
                                    <TableCell width={140}>Abteilung</TableCell>
                                </TableRow>
                            </TableHead>

                            <TableBody>
                                {filtered.map((c) => (
                                    <TableRow
                                        key={c.id}
                                        hover
                                        sx={{ cursor: "pointer" }}
                                        onClick={() => navigate(`/cases/${c.id}`)}
                                    >
                                        <TableCell>{c.id}</TableCell>
                                        <TableCell>{safeText(c.applicantName) || "-"}</TableCell>
                                        <TableCell>{safeText(c.subject) || "-"}</TableCell>
                                        <TableCell>{safeText(c.formType) || "-"}</TableCell>
                                        <TableCell>
                                            <StatusChip status={c.status} />
                                        </TableCell>
                                        <TableCell>{safeText(c.departmentId) || "-"}</TableCell>
                                    </TableRow>
                                ))}

                                {filtered.length === 0 && !error && (
                                    <TableRow>
                                        <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                                            <Typography variant="body2" color="text.secondary">
                                                Keine Fälle gefunden.
                                            </Typography>
                                        </TableCell>
                                    </TableRow>
                                )}
                            </TableBody>
                        </Table>
                    </TableContainer>
                )}
            </Paper>
        </Box>
    );
}
