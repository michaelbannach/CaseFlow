import Chip from "@mui/material/Chip";

function mapStatusToChipProps(status) {

    const label = status || "Unbekannt";

    
    const normalized = String(label);

    
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
