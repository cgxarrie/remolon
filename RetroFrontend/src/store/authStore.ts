import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Role } from '../types';

function parseJwt(token: string): { userId: string | null; organizationId: string | null } {
    try {
        const payload = token.split('.')[1];
        if (!payload) return { userId: null, organizationId: null };
        const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
        // The backend sets ClaimTypes.NameIdentifier and "sub" both to the user ID
        return {
            userId: (json['sub'] as string | undefined) ?? null,
            organizationId: (json['organizationId'] as string | undefined) ?? null,
        };
    } catch {
        return { userId: null, organizationId: null };
    }
}

interface AuthState {
    token: string | null;
    userId: string | null;
    organizationId: string | null;
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
            organizationId: null,
            email: null,
            role: null,
            nickname: null,
            setAuth: (token, email, role, nickname) => {
                const claims = parseJwt(token);
                set({ token, ...claims, email, role: role as Role, nickname });
            },
            clearAuth: () => set({ token: null, userId: null, organizationId: null, email: null, role: null, nickname: null }),
        }),
        { name: 'retro-auth' }
    )
);
