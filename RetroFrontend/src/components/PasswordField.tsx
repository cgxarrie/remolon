import { useState, type InputHTMLAttributes } from 'react';

export function getPasswordMatchState(password: string, confirmPassword: string) {
    const passwordsMatch = password.length > 0 && confirmPassword.length > 0 && password === confirmPassword;
    const passwordsMismatch = confirmPassword.length > 0 && password !== confirmPassword;
    return { passwordsMatch, passwordsMismatch };
}

function PasswordVisibilityToggle({
    visible,
    onToggle,
    label,
}: {
    visible: boolean;
    onToggle: () => void;
    label: string;
}) {
    return (
        <button
            type="button"
            onClick={onToggle}
            aria-label={label}
            className="absolute inset-y-0 right-0 px-2.5 text-slate-500 hover:text-slate-700"
        >
            {visible ? (
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden="true">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M3 3l18 18M10.5 10.7a2.5 2.5 0 003.8 3.2M9.9 5.2A10.4 10.4 0 0112 5c5 0 9.3 3.1 11 7.5a12 12 0 01-4.1 5.1M6.6 6.6A12 12 0 001 12.5C2.7 16.9 7 20 12 20a10.8 10.8 0 005.1-1.3" />
                </svg>
            ) : (
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden="true">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M2 12.5C3.7 8.1 7.9 5 12 5s8.3 3.1 10 7.5C20.3 16.9 16.1 20 12 20S3.7 16.9 2 12.5z" />
                    <circle cx="12" cy="12.5" r="2.5" />
                </svg>
            )}
        </button>
    );
}

type PasswordFieldProps = {
    value: string;
    onChange: (value: string) => void;
    toggleLabel?: string;
    matchState?: 'none' | 'match' | 'mismatch';
} & Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'value' | 'onChange' | 'className'>;

export function PasswordField({
    value,
    onChange,
    toggleLabel = 'password',
    matchState = 'none',
    ...inputProps
}: PasswordFieldProps) {
    const [visible, setVisible] = useState(false);
    const hideLabel = `Hide ${toggleLabel}`;
    const showLabel = `Show ${toggleLabel}`;

    const borderClass =
        matchState === 'mismatch'
            ? 'border-red-400 focus:ring-red-500'
            : matchState === 'match'
              ? 'border-emerald-400 focus:ring-emerald-500'
              : 'border-slate-300 focus:ring-indigo-500';

    return (
        <div className="relative">
            <input
                {...inputProps}
                type={visible ? 'text' : 'password'}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                aria-invalid={matchState === 'mismatch' ? true : inputProps['aria-invalid']}
                className={`w-full border rounded-md px-3 py-2 pr-10 text-sm focus:outline-none focus:ring-2 ${borderClass}`}
            />
            <PasswordVisibilityToggle
                visible={visible}
                onToggle={() => setVisible((v) => !v)}
                label={visible ? hideLabel : showLabel}
            />
        </div>
    );
}

export function PasswordMatchStatus({
    passwordsMatch,
    confirmPassword,
    id = 'password-match-status',
}: {
    passwordsMatch: boolean;
    confirmPassword: string;
    id?: string;
}) {
    if (confirmPassword.length === 0) return null;

    return (
        <p
            id={id}
            role="status"
            className={`mt-1 text-xs ${passwordsMatch ? 'text-emerald-600' : 'text-red-600'}`}
        >
            {passwordsMatch ? 'Passwords match.' : 'Passwords do not match.'}
        </p>
    );
}
