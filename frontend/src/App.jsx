import { Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layout/MainLayout.jsx";
import CaseListPage from "./pages/CaseListPage.jsx";
import CaseCreatePage from "./pages/CaseCreatePage.jsx";

export default function App() {
    return (
        <Routes>
            <Route element={<MainLayout />}>
                <Route path="/" element={<Navigate to="/cases" replace />} />
                <Route path="/cases" element={<CaseListPage />} />
                <Route path="/cases/new" element={<CaseCreatePage />} />
            </Route>
        </Routes>
    );
}
