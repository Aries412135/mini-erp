import { isAxiosError } from "axios";

interface ProblemDetails {
    title?: string;
    details?: string;
    errors?: Record<string, string[]>;
}

export function getErrorMessage(error: unknown): string {
    if (isAxiosError<ProblemDetails>(error)) {
        if (!error.response) {
            return 'Tidak dapat terhubung ke server. Periksa koneksi lalu coba lagi.';
        }

        const { status, data } = error.response;

        if (status === 401) {
            return data?.details ?? 'Email atau password salah.';
        }

        if (status === 400 && data?.errors) {
            return Object.values(data.errors).flat().join(' ');
        }

        if (status >= 500) {
            return 'Terjadi kesalahan pada server. Coba lagi nanti.';
        }

        return data?.details ?? data?.title ?? 'Terjadi kesalahan.';
    }

    return 'Terjadi kesalahan tak terduga.';
}