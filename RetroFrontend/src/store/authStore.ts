import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Role } from '../types';

function parseJwtSub(token: string): string | null {
    try {
        const payload = token.split('.')[1];
        if (!payload) return null;
        const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
        // The backend sets ClaimTypes.NameIdentifier and "sub" both to the user ID
        return (json['sub'] as string | undefined) ?? null;
    } catch {
        return null;
    }
}

interface AuthState {
    token: string | null;
    userId: string | null;
    email: string | null;
    role: Role | null;
    nickname: string | null;
    setAuth: (token: string, email: string, role: string, nickname: string) => void;
    clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            token: null,
            userId: null,
            email: null,
            role: null,
            nickname: null,
            setAuth: (token, email, role, nickname) =>
                set({ token, userId: parseJwtSub(token), email, role: role as Role, nickname }),
            clearAuth: () => set({ token: null, userId: null, email: null, role: null, nickname: null }),
        }),
        { name: 'retro-auth' }
    )
);
