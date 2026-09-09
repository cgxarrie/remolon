import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthTokenResponse, Role } from '../types';

interface AuthState {
    userId: string | null;
    organizationId: string | null;
    organizationName: string | null;
    email: string | null;
    role: Role | null;
    nickname: string | null;
    avatarUrl: string | null;
    setAuth: (session: AuthTokenResponse) => void;
    setOrganizationName: (name: string) => void;
    setProfile: (profile: {
        nickname?: string;
        avatarUrl?: string | null;
        role?: Role;
        userId?: string;
        organizationId?: string | null;
    }) => void;
    clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            userId: null,
            organizationId: null,
            organizationName: null,
            email: null,
            role: null,
            nickname: null,
            avatarUrl: null,
            setAuth: (session) => {
                set((state) => ({
                    userId: session.userId ?? state.userId,
                    organizationId: session.organizationId ?? state.organizationId,
                    organizationName: session.organizationName ?? state.organizationName,
                    email: session.email,
                    role: session.role as Role,
                    nickname: session.nickname,
                    avatarUrl: session.userId === state.userId ? state.avatarUrl : null,
                }));
            },
            setOrganizationName: (name) => set({ organizationName: name }),
            setProfile: (profile) => set((state) => ({
                nickname: profile.nickname ?? state.nickname,
                avatarUrl: profile.avatarUrl === undefined ? state.avatarUrl : profile.avatarUrl,
                role: profile.role ?? state.role,
                userId: profile.userId ?? state.userId,
                organizationId: profile.organizationId === undefined
                    ? state.organizationId
                    : profile.organizationId,
            })),
            clearAuth: () => set({
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
                nickname: state.nickname,
                avatarUrl: state.avatarUrl,
            }),
        }
    )
);
