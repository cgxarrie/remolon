import { useEffect } from 'react';
import {
    applyColorMode,
    prefersDark,
    useColorModeStore,
    type ColorMode,
} from '../store/colorModeStore';

const modes: ColorMode[] = ['light', 'dark', 'system'];

const iconProps = {
    xmlns: 'http://www.w3.org/2000/svg',
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.75,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
    className: 'w-5 h-5',
    'aria-hidden': true,
};

function ModeIcon({ mode }: { mode: ColorMode }) {
    if (mode === 'dark') {
        return (
            <svg {...iconProps}>
                <path d="M21 14.3A8.5 8.5 0 1 1 9.7 3 7 7 0 0 0 21 14.3z" />
            </svg>
        );
    }

    if (mode === 'system') {
        return (
            <svg {...iconProps}>
                <rect x="3" y="4" width="18" height="12" rx="2" />
                <path d="M8 20h8" />
                <path d="M12 16v4" />
            </svg>
        );
    }

    return (
        <svg {...iconProps}>
            <circle cx="12" cy="12" r="4" />
            <path d="M12 3v2" />
            <path d="M12 19v2" />
            <path d="M5.6 5.6l1.4 1.4" />
            <path d="M17 17l1.4 1.4" />
            <path d="M3 12h2" />
            <path d="M19 12h2" />
            <path d="M5.6 18.4l1.4-1.4" />
            <path d="M17 7l1.4-1.4" />
        </svg>
    );
}

function nextMode(mode: ColorMode): ColorMode {
    return modes[(modes.indexOf(mode) + 1) % modes.length];
}

function modeLabel(mode: ColorMode): string {
    if (mode === 'dark') return 'Dark';
    if (mode === 'light') return 'Light';
    return 'System';
}

export function ColorModeSync() {
    const mode = useColorModeStore((state) => state.mode);

    useEffect(() => {
        applyColorMode(mode);
        if (mode !== 'system') return;

        const media = window.matchMedia('(prefers-color-scheme: dark)');
        const onChange = () => applyColorMode('system');
        media.addEventListener('change', onChange);
        return () => media.removeEventListener('change', onChange);
    }, [mode]);

    return null;
}

export function ColorModeToggle({ variant = 'page' }: { variant?: 'header' | 'page' }) {
    const mode = useColorModeStore((state) => state.mode);
    const setMode = useColorModeStore((state) => state.setMode);
    const next = nextMode(mode);
    const resolved = mode === 'system' ? (prefersDark() ? 'dark' : 'light') : mode;

    return (
        <button
            type="button"
            onClick={() => setMode(next)}
            aria-label={`Color mode: ${modeLabel(mode)}. Switch to ${modeLabel(next)}`}
            title={`Appearance: ${modeLabel(mode)}${mode === 'system' ? ` (${resolved})` : ''}. Click for ${modeLabel(next).toLowerCase()}.`}
            className={
                variant === 'header'
                    ? 'p-1.5 theme-header-hover rounded-md transition-colors'
                    : 'p-1.5 rounded-md text-slate-600 hover:text-slate-900 hover:bg-slate-200 dark:text-slate-300 dark:hover:text-white dark:hover:bg-slate-800 transition-colors'
            }
        >
            <ModeIcon mode={mode} />
        </button>
    );
}

export function AppearanceOptions() {
    const mode = useColorModeStore((state) => state.mode);
    const setMode = useColorModeStore((state) => state.setMode);

    return (
        <div className="flex flex-wrap gap-2">
            {modes.map((option) => (
                <button
                    key={option}
                    type="button"
                    aria-pressed={mode === option}
                    onClick={() => setMode(option)}
                    className={`inline-flex items-center gap-2 rounded-md border px-3 py-2 text-sm ${
                        mode === option
                            ? 'border-transparent theme-primary'
                            : 'border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800'
                    }`}
                >
                    <ModeIcon mode={option} />
                    {modeLabel(option)}
                </button>
            ))}
        </div>
    );
}
