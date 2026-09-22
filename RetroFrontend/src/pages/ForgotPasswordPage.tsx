import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { AuthShell } from '../components/AuthShell';
import { authApi } from '../api/auth';

function emailFromNavigation(state: unknown): string {
    if (state && typeof state === 'object' && 'email' in state && typeof state.email === 'string') {
        return state.email.trim();
    }
    return '';
}

export function ForgotPasswordPage() {
    const location = useLocation();
    const [email, setEmail] = useState(() => emailFromNavigation(location.state));
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleRequestReset(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setMessage('');
        setLoading(true);

        try {
            const res = await authApi.forgotPassword({ email: email.trim() });
            setMessage(res.message);
        } catch {
            setError('Unable to process reset request. Try again.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <AuthShell>
            <div className="bg-white dark:bg-slate-900 rounded-xl shadow-lg p-8 w-full max-w-md">
                <h1 className="text-2xl font-bold text-indigo-700 dark:text-indigo-300 mb-6 text-center">Forgot Password</h1>

                {message && (
                    <p className="mb-4 text-sm text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-950/40 border border-green-200 dark:border-green-800 rounded-md p-3">
                        {message}
                    </p>
                )}
                {error && (
                    <p className="mb-4 text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900 rounded-md p-3">
                        {error}
                    </p>
                )}

                <form onSubmit={handleRequestReset} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-200 mb-1">Email</label>
                        <input
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            className="w-full border border-slate-300 dark:border-slate-600 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    <button
                        type="submit"
                        disabled={loading || !email.trim()}
                        className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-medium py-2 rounded-md transition-colors"
                    >
                        {loading ? 'Sending…' : 'Send reset email'}
                    </button>
                </form>

                <p className="mt-4 text-center text-sm text-slate-500 dark:text-slate-400">
                    Back to{' '}
                    <Link to="/login" className="text-indigo-600 dark:text-indigo-400 hover:underline">
                        Sign In
                    </Link>
                </p>
            </div>
        </AuthShell>
    );
}
