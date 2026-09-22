import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type ColorMode = 'light' | 'dark' | 'system';

export const COLOR_MODE_STORAGE_KEY = 'retro-color-mode';

function readStoredMode(): ColorMode {
    try {
        const raw = localStorage.getItem(COLOR_MODE_STORAGE_KEY);
        if (!raw) return 'system';
        const parsed = JSON.parse(raw) as { state?: { mode?: unknown } } | ColorMode;
        const mode = typeof parsed === 'string' ? parsed : parsed.state?.mode;
        if (mode === 'light' || mode === 'dark' || mode === 'system') return mode;
    } catch {
        /* keep the default */
    }
    return 'system';
}

export function prefersDark(): boolean {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

export function isDarkMode(mode: ColorMode): boolean {
    return mode === 'dark' || (mode === 'system' && prefersDark());
}

export function applyColorMode(mode: ColorMode) {
    const dark = isDarkMode(mode);
    document.documentElement.classList.toggle('dark', dark);
    document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
}

interface ColorModeState {
    mode: ColorMode;
    setMode: (mode: ColorMode) => void;
}

export const useColorModeStore = create<ColorModeState>()(
    persist(
        (set) => ({
            mode: readStoredMode(),
            setMode: (mode) => {
                applyColorMode(mode);
                set({ mode });
            },
        }),
        {
            name: COLOR_MODE_STORAGE_KEY,
            onRehydrateStorage: () => (state) => {
                applyColorMode(state?.mode ?? 'system');
            },
        },
    ),
);
