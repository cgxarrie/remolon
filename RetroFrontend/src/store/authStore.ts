import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Role } from '../types';

interface JwtClaims {
    userId: string | null;
    organizationId: string | null;
    organizationName: string | null;
    exp: number | null;
}

const emptyClaims: JwtClaims = { userId: null, organizationId: null, organizationName: null, exp: null };

function parseJwt(token: string): JwtClaims {
    try {
        const payload = token.split('.')[1];
        if (!payload) return emptyClaims;
        const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
        return {
            userId: (json['sub'] as string | undefined) ?? null,
            organizationId: (json['organizationId'] as string | undefined) ?? null,
            organizationName: (json['organizationName'] as string | undefined) ?? null,
            exp: typeof json.exp === 'number' ? json.exp : null,
        };
    } catch {
        return emptyClaims;
    }
}

export function isAccessTokenExpired(token: string): boolean {
    const exp = parseJwt(token).exp;
    if (exp == null) return false;
    return exp * 1000 <= Date.now();
}

interface AuthState {
    token: string | null;
    refreshToken: string | null;
    userId: string | null;
    organizationId: string | null;
    organizationName: string | null;
    email: string | null;
    role: Role | null;
    nickname: string | null;
    avatarUrl: string | null;
    setAuth: (
        token: string,
        email: string,
        role: string,
        nickname: string,
        organizationName?: string | null,
        refreshToken?: string | null,
    ) => void;
    setOrganizationName: (name: string) => void;
    setProfile: (profile: { nickname?: string; avatarUrl?: string | null; role?: Role }) => void;
    clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            token: null,
            refreshToken: null,
            userId: null,
            organizationId: null,
            organizationName: null,
            email: null,
            role: null,
            nickname: null,
            avatarUrl: null,
            setAuth: (token, email, role, nickname, organizationName, refreshToken) => {
                const claims = parseJwt(token);
                set((state) => ({
                    token,
                    refreshToken: refreshToken === undefined ? state.refreshToken : refreshToken,
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
                role: profile.role ?? state.role,
            })),
            clearAuth: () => set({
                token: null,
                refreshToken: null,
                userId: null,
                organizationId: null,
                organizationName: null,
                email: null,
                role: null,
                nickname: null,
                avatarUrl: null,
            }),
        }),
        {
            name: 'retro-auth',
            partialize: (state) => ({
                userId: state.userId,
                organizationId: state.organizationId,
                organizationName: state.organizationName,
                email: state.email,
                role: state.role,
                nickname: state.nickname,
                avatarUrl: state.avatarUrl,
            }),
        }
    )
);
