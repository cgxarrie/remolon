import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Role } from '../types';

interface JwtClaims {
    userId: string | null;
    organizationId: string | null;
    organizationName: string | null;
}

const emptyClaims: JwtClaims = { userId: null, organizationId: null, organizationName: null };

function parseJwt(token: string): JwtClaims {
    try {
        const payload = token.split('.')[1];
        if (!payload) return emptyClaims;
        const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
        // The backend sets ClaimTypes.NameIdentifier and "sub" both to the user ID
        return {
            userId: (json['sub'] as string | undefined) ?? null,
            organizationId: (json['organizationId'] as string | undefined) ?? null,
            organizationName: (json['organizationName'] as string | undefined) ?? null,
        };
    } catch {
        return emptyClaims;
    }
}

interface AuthState {
    token: string | null;
    userId: string | null;
    organizationId: string | null;
    organizationName: string | null;
    email: string | null;
    role: Role | null;
    nickname: string | null;
    avatarUrl: string | null;
    setAuth: (token: string, email: string, role: string, nickname: string, organizationName?: string | null) => void;
    setOrganizationName: (name: string) => void;
    setProfile: (profile: { nickname?: string; avatarUrl?: string | null }) => void;
    clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            token: null,
            userId: null,
            organizationId: null,
            organizationName: null,
            email: null,
            role: null,
            nickname: null,
            avatarUrl: null,
            setAuth: (token, email, role, nickname, organizationName) => {
                const claims = parseJwt(token);
                set((state) => ({
                    token,
                    userId: claims.userId,
                    organizationId: claims.organizationId,
                    organizationName: organizationName ?? claims.organizationName,
                    email,
                    role: role as Role,
                    nickname,
                    avatarUrl: claims.userId === state.userId ? state.avatarUrl : null,
                }));
            },
            setOrganizationName: (name) => set({ organizationName: name }),
            setProfile: (profile) => set((state) => ({
                nickname: profile.nickname ?? state.nickname,
                avatarUrl: profile.avatarUrl === undefined ? state.avatarUrl : profile.avatarUrl,
            })),
            clearAuth: () => set({
                token: null,
                userId: null,
                organizationId: null,
                organizationName: null,
                email: null,
                role: null,
                nickname: null,
                avatarUrl: null,
            }),
        }),
        { name: 'retro-auth' }
    )
);
