import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { organizationsApi } from './api/organizations';
import { useAuthStore } from './store/authStore';
import type { OrganizationTheme, PresetThemeKey } from './types';

export type ThemePalette = {
    name: string;
    header: string;
    headerHover: string;
    accent: string;
    accentHover: string;
    focus: string;
};

// Ordered by colour family so the picker reads as a spectrum. Accents sit on white text,
// so warm hues use darker shades than their header to stay legible.
export const themePresets: Record<PresetThemeKey, ThemePalette> = {
    default: {
        name: 'Indigo',
        header: '#4338ca',
        headerHover: '#4f46e5',
        accent: '#4f46e5',
        accentHover: '#4338ca',
        focus: '#6366f1',
    },
    azure: {
        name: 'Azure',
        header: '#1e40af',
        headerHover: '#1d4ed8',
        accent: '#2563eb',
        accentHover: '#1d4ed8',
        focus: '#60a5fa',
    },
    ocean: {
        name: 'Ocean',
        header: '#075985',
        headerHover: '#0369a1',
        accent: '#0284c7',
        accentHover: '#0369a1',
        focus: '#38bdf8',
    },
    lagoon: {
        name: 'Lagoon',
        header: '#155e75',
        headerHover: '#0e7490',
        accent: '#0891b2',
        accentHover: '#0e7490',
        focus: '#22d3ee',
    },
    teal: {
        name: 'Teal',
        header: '#115e59',
        headerHover: '#0f766e',
        accent: '#0d9488',
        accentHover: '#0f766e',
        focus: '#2dd4bf',
    },
    emerald: {
        name: 'Emerald',
        header: '#065f46',
        headerHover: '#047857',
        accent: '#059669',
        accentHover: '#047857',
        focus: '#34d399',
    },
    forest: {
        name: 'Forest',
        header: '#166534',
        headerHover: '#15803d',
        accent: '#16a34a',
        accentHover: '#15803d',
        focus: '#4ade80',
    },
    meadow: {
        name: 'Meadow',
        header: '#3f6212',
        headerHover: '#4d7c0f',
        accent: '#4d7c0f',
        accentHover: '#3f6212',
        focus: '#a3e635',
    },
    marigold: {
        name: 'Marigold',
        header: '#854d0e',
        headerHover: '#a16207',
        accent: '#a16207',
        accentHover: '#854d0e',
        focus: '#facc15',
    },
    amber: {
        name: 'Amber',
        header: '#92400e',
        headerHover: '#b45309',
        accent: '#b45309',
        accentHover: '#92400e',
        focus: '#fbbf24',
    },
    tangerine: {
        name: 'Tangerine',
        header: '#9a3412',
        headerHover: '#c2410c',
        accent: '#ea580c',
        accentHover: '#c2410c',
        focus: '#fb923c',
    },
    sunset: {
        name: 'Sunset',
        header: '#9f1239',
        headerHover: '#be123c',
        accent: '#d97706',
        accentHover: '#b45309',
        focus: '#fb7185',
    },
    ember: {
        name: 'Ember',
        header: '#7f1d1d',
        headerHover: '#991b1b',
        accent: '#ea580c',
        accentHover: '#c2410c',
        focus: '#fca5a5',
    },
    crimson: {
        name: 'Crimson',
        header: '#991b1b',
        headerHover: '#b91c1c',
        accent: '#dc2626',
        accentHover: '#b91c1c',
        focus: '#f87171',
    },
    rose: {
        name: 'Rose',
        header: '#9f1239',
        headerHover: '#be123c',
        accent: '#e11d48',
        accentHover: '#be123c',
        focus: '#fb7185',
    },
    blossom: {
        name: 'Blossom',
        header: '#9d174d',
        headerHover: '#be185d',
        accent: '#db2777',
        accentHover: '#be185d',
        focus: '#f472b6',
    },
    orchid: {
        name: 'Orchid',
        header: '#86198f',
        headerHover: '#a21caf',
        accent: '#c026d3',
        accentHover: '#a21caf',
        focus: '#e879f9',
    },
    plum: {
        name: 'Plum',
        header: '#6b21a8',
        headerHover: '#7e22ce',
        accent: '#9333ea',
        accentHover: '#7e22ce',
        focus: '#c084fc',
    },
    violet: {
        name: 'Violet',
        header: '#5b21b6',
        headerHover: '#6d28d9',
        accent: '#7c3aed',
        accentHover: '#6d28d9',
        focus: '#a78bfa',
    },
    aurora: {
        name: 'Aurora',
        header: '#4c1d95',
        headerHover: '#5b21b6',
        accent: '#0891b2',
        accentHover: '#0e7490',
        focus: '#a78bfa',
    },
    midnight: {
        name: 'Midnight',
        header: '#0f172a',
        headerHover: '#1e293b',
        accent: '#1d4ed8',
        accentHover: '#1e40af',
        focus: '#64748b',
    },
    slate: {
        name: 'Slate',
        header: '#334155',
        headerHover: '#475569',
        accent: '#475569',
        accentHover: '#334155',
        focus: '#94a3b8',
    },
    graphite: {
        name: 'Graphite',
        header: '#27272a',
        headerHover: '#3f3f46',
        accent: '#52525b',
        accentHover: '#3f3f46',
        focus: '#a1a1aa',
    },
    mocha: {
        name: 'Mocha',
        header: '#44403c',
        headerHover: '#57534e',
        accent: '#78716c',
        accentHover: '#57534e',
        focus: '#a8a29e',
    },
};

export const presetThemeKeys = Object.keys(themePresets) as PresetThemeKey[];

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
    if (theme.themeKey !== 'custom') return themePresets[theme.themeKey] ?? themePresets.default;

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
    const organizationId = useAuthStore((state) => state.organizationId);
    const { data: organization } = useQuery({
        queryKey: ['organizationTheme', organizationId],
        queryFn: () => organizationsApi.getById(organizationId!),
        enabled: !!organizationId,
    });

    useEffect(() => {
        applyTheme(organizationId ? resolveTheme(organization?.theme) : themePresets.default);
    }, [organizationId, organization?.theme]);

    return children;
}
