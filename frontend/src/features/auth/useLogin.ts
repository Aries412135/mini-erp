import { useNavigate } from "react-router-dom";
import { useAuthStore } from "./authStore";
import { useMutation } from "@tanstack/react-query";
import type { LoginFormValues } from "./loginSchema";
import { authApi } from "../../api/authApi";


export function useLogin() {
    const navigate = useNavigate();
    const setTokens = useAuthStore((s) => s.setTokens);
    const setUser = useAuthStore((s) => s.setUser);
    const clear = useAuthStore((s) => s.clear);

    return useMutation({
        mutationFn: async (values: LoginFormValues) => {
            const tokens = await authApi.login(values);
            setTokens(tokens);
            return authApi.me();
        },
        onSuccess: (user) => {
            setUser(user);
            navigate('/dashboard', { replace: true });
        },
        onError: () => {
            clear(); 
        }
    })
}