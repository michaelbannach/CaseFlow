import { Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layout/MainLayout";
import CaseListPage from "./pages/CaseListPage";
import CaseCreatePage from "./pages/CaseCreatePage";
import CaseDetailPage from "./pages/CaseDetailPage"; // NEU
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";

export default function App() {
    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            <Route element={<MainLayout />}>
                {/* konsistente URL-Struktur */}
                <Route path="/" element={<Navigate to="/cases" replace />} />
                <Route path="/cases" element={<CaseListPage />} />
                <Route path="/cases/new" element={<CaseCreatePage />} />
                <Route path="/cases/:id" element={<CaseDetailPage />} /> 
            </Route>

            <Route path="*" element={<Navigate to="/cases" replace />} />
        </Routes>
    );
}
