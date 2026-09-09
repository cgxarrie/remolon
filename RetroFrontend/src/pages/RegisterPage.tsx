import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/auth';
import { PasswordField, PasswordMatchStatus, getPasswordMatchState } from '../components/PasswordField';
import { PASSWORD_MIN_LENGTH } from '../types';
import { useAuthStore } from '../store/authStore';

        // The API reports failures as { message } or ASP.NET validation problem details.
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

export function RegisterPage() {
    const navigate = useNavigate();
    const setAuth = useAuthStore((s) => s.setAuth);
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [nickname, setNickname] = useState('');
    const [organizationName, setOrganizationName] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const { passwordsMatch, passwordsMismatch } = getPasswordMatchState(password, confirmPassword);

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
            setAuth(res);
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
                            <span className="text-slate-400 font-normal">(min {PASSWORD_MIN_LENGTH} chars, upper, lower, number, symbol)</span>
                        </label>
                        <PasswordField
                            value={password}
                            onChange={setPassword}
                            required
                            minLength={PASSWORD_MIN_LENGTH}
                            autoComplete="new-password"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Confirm Password</label>
                        <PasswordField
                            value={confirmPassword}
                            onChange={setConfirmPassword}
                            required
                            minLength={PASSWORD_MIN_LENGTH}
                            autoComplete="new-password"
                            toggleLabel="confirm password"
                            aria-describedby="password-match-status"
                            matchState={passwordsMismatch ? 'mismatch' : passwordsMatch ? 'match' : 'none'}
                        />
                        <PasswordMatchStatus passwordsMatch={passwordsMatch} confirmPassword={confirmPassword} />
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
