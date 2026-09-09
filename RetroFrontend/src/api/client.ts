import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../store/authStore';
import type { AuthTokenResponse } from '../types';

const client = axios.create({
    baseURL: '/api',
    withCredentials: true,
    headers: { 'Content-Type': 'application/json' },
});

type RetryConfig = InternalAxiosRequestConfig & { _retry?: boolean };

let refreshInFlight: Promise<boolean> | null = null;

function applyAuth(data: AuthTokenResponse) {
    useAuthStore.getState().setAuth(
        data.token,
        data.email,
        data.role,
        data.nickname,
        data.organizationName,
        data.refreshToken,
    );
}

async function refreshSession(): Promise<boolean> {
    try {
        const { data } = await axios.post<AuthTokenResponse>(
            '/api/auth/refresh',
            {},
            { withCredentials: true },
        );
        applyAuth(data);
        return true;
    } catch {
        return false;
    }
}

function isAuthCredentialRequest(url: string | undefined) {
    if (!url) return false;
    return (
        url.includes('/auth/login') ||
        url.includes('/auth/register') ||
        url.includes('/auth/refresh') ||
        url.includes('/auth/forgot-password') ||
        url.includes('/auth/reset-password') ||
        url.includes('/auth/change-initial-password')
    );
}

client.interceptors.request.use((config) => {
    const token = useAuthStore.getState().token;
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    if (config.data instanceof FormData) {
        delete config.headers['Content-Type'];
    }
    return config;
});

client.interceptors.response.use(
    (res) => res,
    async (error: AxiosError) => {
        const original = error.config as RetryConfig | undefined;
        if (error.response?.status !== 401 || !original || original._retry || isAuthCredentialRequest(original.url)) {
            return Promise.reject(error);
        }

        original._retry = true;
        refreshInFlight ??= refreshSession().finally(() => {
            refreshInFlight = null;
        });
        const refreshed = await refreshInFlight;
        if (!refreshed) {
            useAuthStore.getState().clearAuth();
            return Promise.reject(error);
        }

        original.headers.Authorization = `Bearer ${useAuthStore.getState().token}`;
        return client(original);
    },
);

export default client;
