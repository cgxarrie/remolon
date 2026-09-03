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
    selectedOrganizationId: string | null;
    selectedOrganizationName: string | null;
    email: string | null;
    role: Role | null;
    nickname: string | null;
    setAuth: (token: string, email: string, role: string, nickname: string, organizationName?: string | null) => void;
    setOrganizationName: (name: string) => void;
    setSelectedOrganization: (id: string, name: string) => void;
    clearSelectedOrganization: () => void;
    clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            token: null,
            userId: null,
            organizationId: null,
            organizationName: null,
            selectedOrganizationId: null,
            selectedOrganizationName: null,
            email: null,
            role: null,
            nickname: null,
            setAuth: (token, email, role, nickname, organizationName) => {
                const claims = parseJwt(token);
                set({
                    token,
                    userId: claims.userId,
                    organizationId: claims.organizationId,
                    organizationName: organizationName ?? claims.organizationName,
                    selectedOrganizationId: null,
                    selectedOrganizationName: null,
                    email,
                    role: role as Role,
                    nickname,
                });
            },
            setOrganizationName: (name) => set({ organizationName: name }),
            setSelectedOrganization: (id, name) => set({
                selectedOrganizationId: id,
                selectedOrganizationName: name,
            }),
            clearSelectedOrganization: () => set({
                selectedOrganizationId: null,
                selectedOrganizationName: null,
            }),
            clearAuth: () => set({
                token: null,
                userId: null,
                organizationId: null,
                organizationName: null,
                selectedOrganizationId: null,
                selectedOrganizationName: null,
                email: null,
                role: null,
                nickname: null,
            }),
        }),
        { name: 'retro-auth' }
    )
);
