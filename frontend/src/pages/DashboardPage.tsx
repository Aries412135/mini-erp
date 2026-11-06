import { Link } from "react-router-dom";
import { useAuthStore } from "../features/auth/authStore";

// SEMENTARA: US-107 menggantikan pengecekan ini dengan ProtectedRoute.
export function DashboardPage() {
    const user = useAuthStore((s) => s.user);

    if (!user) {
        return (
            <main className="page">
                <p>Anda belum login.</p>
                <Link to="/login"></Link>
            </main>
        );
    }

    return (
        <main className="page">
            <h1>Dashboard</h1>
            <p>Halo, {user.name}</p>
            <p>Email: {user.email}</p>
            <p>Role: {user.roles.length > 0 ? user.roles.join(', ') : '(Belum ada role)'}</p>
        </main>
    );
}