import * as React from "react";
import Drawer from "@mui/material/Drawer";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Toolbar from "@mui/material/Toolbar";
import Divider from "@mui/material/Divider";

import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import FolderOpenOutlinedIcon from "@mui/icons-material/FolderOpenOutlined";
import AddCircleOutlineOutlinedIcon from "@mui/icons-material/AddCircleOutlineOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";

const drawerWidth = 280;

export default function Sidebar({
                                    mobileOpen,
                                    onCloseMobile,
                                    onNavigate,
                                    currentPath,
                                }) {
    const items = [
        { label: "Dashboard", path: "/", icon: <HomeOutlinedIcon /> },
        { label: "Fälle", path: "/cases", icon: <FolderOpenOutlinedIcon /> },
        { label: "Neuer Fall", path: "/cases/new", icon: <AddCircleOutlineOutlinedIcon /> },
    ];

    const secondary = [{ label: "Einstellungen", path: "/settings", icon: <SettingsOutlinedIcon /> }];

    const drawerContent = (
        <div>
            <Toolbar />
            <Divider />
            <List>
                {items.map((it) => (
                    <ListItemButton
                        key={it.path}
                        selected={currentPath === it.path}
                        onClick={() => {
                            onNavigate(it.path);
                            onCloseMobile?.();
                        }}
                    >
                        <ListItemIcon>{it.icon}</ListItemIcon>
                        <ListItemText primary={it.label} />
                    </ListItemButton>
                ))}
            </List>
            <Divider />
            <List>
                {secondary.map((it) => (
                    <ListItemButton
                        key={it.path}
                        selected={currentPath === it.path}
                        onClick={() => {
                            onNavigate(it.path);
                            onCloseMobile?.();
                        }}
                    >
                        <ListItemIcon>{it.icon}</ListItemIcon>
                        <ListItemText primary={it.label} />
                    </ListItemButton>
                ))}
            </List>
        </div>
    );

    return (
        <>
            {/* Mobile: temporär */}
            <Drawer
                variant="temporary"
                open={mobileOpen}
                onClose={onCloseMobile}
                ModalProps={{ keepMounted: true }}
                sx={{
                    display: { xs: "block", md: "none" },
                    "& .MuiDrawer-paper": { width: drawerWidth },
                }}
            >
                {drawerContent}
            </Drawer>

            {/* Desktop: permanent */}
            <Drawer
                variant="permanent"
                open
                sx={{
                    display: { xs: "none", md: "block" },
                    "& .MuiDrawer-paper": { width: drawerWidth, boxSizing: "border-box" },
                }}
            >
                {drawerContent}
            </Drawer>
        </>
    );
}

export { drawerWidth };
