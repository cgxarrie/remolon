import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { organizationsApi } from './api/organizations';
import { useAuthStore } from './store/authStore';
import type { OrganizationTheme, ThemeKey } from './types';

export type ThemePalette = {
    name: string;
    header: string;
    headerHover: string;
    accent: string;
    accentHover: string;
    focus: string;
};

export const themePresets: Record<Exclude<ThemeKey, 'custom'>, ThemePalette> = {
    default: {
        name: 'Indigo',
        header: '#4338ca',
        headerHover: '#4f46e5',
        accent: '#4f46e5',
        accentHover: '#4338ca',
        focus: '#6366f1',
    },
    ocean: {
        name: 'Ocean',
        header: '#075985',
        headerHover: '#0369a1',
        accent: '#0284c7',
        accentHover: '#0369a1',
        focus: '#38bdf8',
    },
    forest: {
        name: 'Forest',
        header: '#166534',
        headerHover: '#15803d',
        accent: '#16a34a',
        accentHover: '#15803d',
        focus: '#4ade80',
    },
    sunset: {
        name: 'Sunset',
        header: '#9f1239',
        headerHover: '#be123c',
        accent: '#d97706',
        accentHover: '#b45309',
        focus: '#fb7185',
    },
};

export const defaultCustomPalette: ThemePalette = {
    name: 'Custom',
    header: '#334155',
    headerHover: '#475569',
    accent: '#7c3aed',
    accentHover: '#6d28d9',
    focus: '#a78bfa',
};

export function resolveTheme(theme?: OrganizationTheme | null): ThemePalette {
    if (!theme || theme.themeKey === 'default') return themePresets.default;
    if (theme.themeKey !== 'custom') return themePresets[theme.themeKey];

    return {
        name: 'Custom',
        header: theme.headerColor ?? defaultCustomPalette.header,
        headerHover: theme.headerHoverColor ?? defaultCustomPalette.headerHover,
        accent: theme.accentColor ?? defaultCustomPalette.accent,
        accentHover: theme.accentHoverColor ?? defaultCustomPalette.accentHover,
        focus: theme.focusColor ?? defaultCustomPalette.focus,
    };
}

function applyTheme(palette: ThemePalette) {
    const root = document.documentElement;
    root.style.setProperty('--theme-header', palette.header);
    root.style.setProperty('--theme-header-hover', palette.headerHover);
    root.style.setProperty('--theme-accent', palette.accent);
    root.style.setProperty('--theme-accent-hover', palette.accentHover);
    root.style.setProperty('--theme-focus', palette.focus);
}

export function OrganizationThemeProvider({ children }: { children: React.ReactNode }) {
    const token = useAuthStore((state) => state.token);
    const role = useAuthStore((state) => state.role);
    const organizationId = useAuthStore((state) => state.organizationId);
    const selectedOrganizationId = useAuthStore((state) => state.selectedOrganizationId);
    const activeOrganizationId = role === 'Admin' ? selectedOrganizationId : organizationId;
    const { data: organization } = useQuery({
        queryKey: ['organizationTheme', activeOrganizationId],
        queryFn: () => organizationsApi.getById(activeOrganizationId!),
        enabled: !!token && !!activeOrganizationId,
        staleTime: 30_000,
    });

    useEffect(() => {
        applyTheme(activeOrganizationId ? resolveTheme(organization?.theme) : themePresets.default);
    }, [activeOrganizationId, organization?.theme]);

    return children;
}
