import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/auth';
import { useAuthStore } from '../store/authStore';

// The API reports failures three ways: Identity error arrays, a { message } conflict,
// and ASP.NET validation problem details keyed by field name.
function describeRegistrationError(data: unknown): string {
    const fallback = 'Registration failed. Please try again.';
    if (Array.isArray(data)) {
        return data.map((e: { description: string }) => e.description).join(' ') || fallback;
    }
    if (data && typeof data === 'object') {
        const body = data as { message?: unknown; errors?: Record<string, unknown> };
        if (typeof body.message === 'string') return body.message;
        if (body.errors && typeof body.errors === 'object') {
            const messages = Object.values(body.errors).flatMap((value) =>
                Array.isArray(value) ? value.filter((v): v is string => typeof v === 'string') : [],
            );
            if (messages.length > 0) return messages.join(' ');
        }
    }
    return fallback;
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

export function RegisterPage() {
    const navigate = useNavigate();
    const setAuth = useAuthStore((s) => s.setAuth);
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [nickname, setNickname] = useState('');
    const [organizationName, setOrganizationName] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const passwordsMatch = password.length > 0 && confirmPassword.length > 0 && password === confirmPassword;
    const passwordsMismatch = confirmPassword.length > 0 && password !== confirmPassword;

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (passwordsMismatch) {
            setError('Passwords do not match.');
            return;
        }
        setError('');
        setLoading(true);
        try {
            const res = await authApi.register({
                email,
                password,
                nickname: nickname.trim() || undefined,
                organizationName: organizationName.trim(),
            });
            setAuth(res.token, res.email, res.role, res.nickname, res.organizationName);
            navigate('/retrospectives');
        } catch (err: unknown) {
            setError(describeRegistrationError((err as { response?: { data?: unknown } }).response?.data));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-100">
            <div className="bg-white rounded-xl shadow-lg p-8 w-full max-w-sm">
                <h1 className="text-2xl font-bold text-indigo-700 mb-6 text-center">ReMolon</h1>
                <h2 className="text-lg font-semibold mb-4 text-center text-slate-700">Create Account</h2>
                {error && (
                    <p className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                        {error}
                    </p>
                )}
                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Email</label>
                        <input
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Organization name</label>
                        <input
                            type="text"
                            value={organizationName}
                            onChange={(e) => setOrganizationName(e.target.value)}
                            required
                            maxLength={200}
                            placeholder="Your company or team name"
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">
                            Nickname{' '}
                            <span className="text-slate-400 font-normal">(optional)</span>
                        </label>
                        <input
                            type="text"
                            value={nickname}
                            onChange={(e) => setNickname(e.target.value)}
                            maxLength={50}
                            placeholder="How you'll appear on tickets"
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">
                            Password{' '}
                            <span className="text-slate-400 font-normal">(min 8 chars)</span>
                        </label>
                        <div className="relative">
                            <input
                                type={showPassword ? 'text' : 'password'}
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                required
                                minLength={8}
                                autoComplete="new-password"
                                className="w-full border border-slate-300 rounded-md px-3 py-2 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                            <PasswordVisibilityToggle
                                visible={showPassword}
                                onToggle={() => setShowPassword((v) => !v)}
                                label={showPassword ? 'Hide password' : 'Show password'}
                            />
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Confirm Password</label>
                        <div className="relative">
                            <input
                                type={showConfirmPassword ? 'text' : 'password'}
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                required
                                minLength={8}
                                autoComplete="new-password"
                                aria-invalid={passwordsMismatch}
                                aria-describedby="password-match-status"
                                className={`w-full border rounded-md px-3 py-2 pr-10 text-sm focus:outline-none focus:ring-2 ${
                                    passwordsMismatch
                                        ? 'border-red-400 focus:ring-red-500'
                                        : passwordsMatch
                                          ? 'border-emerald-400 focus:ring-emerald-500'
                                          : 'border-slate-300 focus:ring-indigo-500'
                                }`}
                            />
                            <PasswordVisibilityToggle
                                visible={showConfirmPassword}
                                onToggle={() => setShowConfirmPassword((v) => !v)}
                                label={showConfirmPassword ? 'Hide confirm password' : 'Show confirm password'}
                            />
                        </div>
                        {confirmPassword.length > 0 && (
                            <p
                                id="password-match-status"
                                role="status"
                                className={`mt-1 text-xs ${passwordsMatch ? 'text-emerald-600' : 'text-red-600'}`}
                            >
                                {passwordsMatch ? 'Passwords match.' : 'Passwords do not match.'}
                            </p>
                        )}
                    </div>
                    <button
                        type="submit"
                        disabled={loading || passwordsMismatch}
                        className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-medium py-2 rounded-md transition-colors"
                    >
                        {loading ? 'Creating account…' : 'Register'}
                    </button>
                </form>
                <p className="mt-4 text-center text-sm text-slate-500">
                    Already have an account?{' '}
                    <Link to="/login" className="text-indigo-600 hover:underline">
                        Sign In
                    </Link>
                </p>
            </div>
        </div>
    );
}
