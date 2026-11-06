import type { AuthTokens, AuthUser, LoginRequest } from "../types/auth";
import axiosClient from "./axiosClient";

export const authApi = {
    login: async (request: LoginRequest) => {
        const { data } = await axiosClient.post<AuthTokens>('/api/auth/login', request);
        return data;
    },

    me: async () => {
        const { data } = await axiosClient.get<AuthUser>('/api/auth/me');
        return data;
    },
};