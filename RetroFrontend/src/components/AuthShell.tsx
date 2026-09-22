import { ColorModeToggle } from './ColorModeToggle';

export function AuthShell({ children }: { children: React.ReactNode }) {
    return (
        <div className="relative min-h-screen flex items-center justify-center bg-slate-100 dark:bg-slate-800 dark:bg-slate-950 p-4">
            <div className="absolute top-4 right-4">
                <ColorModeToggle />
            </div>
            {children}
        </div>
    );
}
