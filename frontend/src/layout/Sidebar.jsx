import * as React from "react";
import Drawer from "@mui/material/Drawer";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Toolbar from "@mui/material/Toolbar";
import Divider from "@mui/material/Divider";
import Box from "@mui/material/Box";

import FolderOpenOutlinedIcon from "@mui/icons-material/FolderOpenOutlined";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";

const drawerWidth = 280;

export default function Sidebar({ mobileOpen, onCloseMobile, onNavigate, currentPath }) {
    function handleLogout() {
        localStorage.removeItem("caseflow_token");
        window.location.href = "/login";
    }

    const drawerContent = (
        <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
            <Toolbar />
            <Divider />

            <List>
                <ListItemButton
                    selected={currentPath === "/cases"}
                    onClick={() => {
                        onNavigate("/cases");
                        onCloseMobile?.();
                    }}
                >
                    <ListItemIcon>
                        <FolderOpenOutlinedIcon />
                    </ListItemIcon>
                    <ListItemText primary="Fälle" />
                </ListItemButton>
            </List>

            <Divider sx={{ mt: "auto" }} />

            <List>
                <ListItemButton onClick={handleLogout} sx={{ color: "error.main" }}>
                    <ListItemIcon sx={{ color: "error.main" }}>
                        <LogoutOutlinedIcon />
                    </ListItemIcon>
                    <ListItemText primary="Logout" />
                </ListItemButton>
            </List>
        </Box>
    );

    return (
        <>
            {/* Mobile */}
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

            {/* Desktop */}
            <Drawer
                variant="permanent"
                open
                sx={{
                    display: { xs: "none", md: "block" },
                    "& .MuiDrawer-paper": {
                        width: drawerWidth,
                        boxSizing: "border-box",
                    },
                }}
            >
                {drawerContent}
            </Drawer>
        </>
    );
}

export { drawerWidth };
