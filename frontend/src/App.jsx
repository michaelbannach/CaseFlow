import { Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layout/MainLayout";
import CaseListPage from "./pages/CaseListPage";
import CaseCreatePage from "./pages/CaseCreatePage";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";

export default function App() {
    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            <Route element={<MainLayout />}>
                <Route path="/" element={<CaseListPage />} />
                <Route path="/cases/new" element={<CaseCreatePage />} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
}
