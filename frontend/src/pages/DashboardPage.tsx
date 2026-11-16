import { Link } from "react-router-dom";
import { useAuthStore } from "../features/auth/authStore";

// SEMENTARA: US-107 menggantikan pengecekan ini dengan ProtectedRoute.
export function DashboardPage() {
    const user = useAuthStore((s) => s.user);

    if (!user) {
        return (
            <main className="mx-auto mt-12 max-w-xl px-4">
                <p className="text-slate-700">Anda belum login.</p>
                <Link to="/login" className="text-blue-600 underline"></Link>
            </main>
        );
    }

    return (
        <main className="mx-auto mt-12 max-w-xl space-y-1 px-4">
            <h1 className="mb-3 text-2xl font-bold text-slate-900">Dashboard</h1>
            <p>Halo, {user.name}</p>
            <p>Email: {user.email}</p>
            <p>Role: {user.roles.length > 0 ? user.roles.join(', ') : '(Belum ada role)'}</p>
        </main>
    );
}