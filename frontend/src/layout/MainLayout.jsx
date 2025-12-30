import * as React from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";

import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import IconButton from "@mui/material/IconButton";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";

import MenuIcon from "@mui/icons-material/Menu";

import Sidebar, { drawerWidth } from "./Sidebar";

export default function MainLayout() {
    const [mobileOpen, setMobileOpen] = React.useState(false);
    const navigate = useNavigate();
    const location = useLocation();

    const handleDrawerToggle = () => setMobileOpen((prev) => !prev);

    return (
        <Box sx={{ display: "flex", minHeight: "100vh" }}>
            <CssBaseline />

            <AppBar
                position="fixed"
                sx={{
                    width: { md: `calc(100% - ${drawerWidth}px)` },
                    ml: { md: `${drawerWidth}px` },
                }}
            >
                <Toolbar>
                    <IconButton
                        color="inherit"
                        edge="start"
                        onClick={handleDrawerToggle}
                        sx={{ mr: 2, display: { md: "none" } }}
                        aria-label="open sidebar"
                    >
                        <MenuIcon />
                    </IconButton>

                    <Typography variant="h6" noWrap component="div">
                        CaseFlow
                    </Typography>
                </Toolbar>
            </AppBar>

            <Sidebar
                mobileOpen={mobileOpen}
                onCloseMobile={() => setMobileOpen(false)}
                onNavigate={(path) => navigate(path)}
                currentPath={location.pathname}
            />

            <Box
                component="main"
                sx={{
                    flexGrow: 1,
                    p: 2,
                    width: { md: `calc(100% - ${drawerWidth}px)` },
                    ml: { md: `${drawerWidth}px` },
                }}
            >
                {/* Abstand unter AppBar */}
                <Toolbar />
                <Outlet />
            </Box>
        </Box>
    );
}
