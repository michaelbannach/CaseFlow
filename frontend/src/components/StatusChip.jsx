import Chip from "@mui/material/Chip";

function mapStatusToChipProps(status) {
    // keine festen Farben nötig; MUI defaults reichen.
    // Du kannst später je Status "color" setzen (success, warning, etc.), wenn du willst.
    const label = status || "Unbekannt";

    // Optional: leichte Normalisierung (falls Backend Enum anders serialisiert)
    const normalized = String(label);

    // Beispielhafte MUI-Farbwahl (optional, aber hilfreich)
    if (normalized === "Erledigt") return { label: normalized, color: "success" };
    if (normalized === "InBearbeitung") return { label: normalized, color: "info" };
    if (normalized === "InKlaerung") return { label: normalized, color: "warning" };
    if (normalized === "Neu") return { label: normalized, color: "default" };

    return { label: normalized, color: "default" };
}

export default function StatusChip({ status, size = "small" }) {
    const props = mapStatusToChipProps(status);
    return <Chip size={size} variant="outlined" {...props} />;
}
