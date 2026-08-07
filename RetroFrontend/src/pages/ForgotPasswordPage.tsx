import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../api/auth';

export function ForgotPasswordPage() {
    const [email, setEmail] = useState('');
    const [token, setToken] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const [step, setStep] = useState<'request' | 'reset'>('request');

    function extractApiErrorMessage(err: unknown, fallback: string): string {
        const axiosErr = err as {
            response?: {
                data?: { message?: string } | Array<{ code?: string; description?: string }>;
            };
        };

        const data = axiosErr.response?.data;
        if (Array.isArray(data)) {
            const description = data
                .map((e) => e.description)
                .filter((d): d is string => Boolean(d))
                .join(' ');
            if (description) return description;
        }

        if (data && typeof data === 'object' && 'message' in data && typeof data.message === 'string') {
            return data.message;
        }

        return fallback;
    }

    async function handleRequestReset(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setMessage('');
        setLoading(true);

        try {
            const res = await authApi.forgotPassword({ email: email.trim() });
            setMessage(res.message);
            if (res.resetToken) {
                setToken(res.resetToken);
                setStep('reset');
            }
        } catch (err: unknown) {
            setError(extractApiErrorMessage(err, 'Unable to process reset request. Try again.'));
        } finally {
            setLoading(false);
        }
    }

    async function handleResetPassword(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setMessage('');

        if (!token.trim()) {
            setError('Reset token is required.');
            return;
        }

        if (newPassword !== confirmPassword) {
            setError('New password and confirmation do not match.');
            return;
        }

        setLoading(true);
        try {
            await authApi.resetPassword({
                email: email.trim(),
                token: token.trim(),
                newPassword,
            });
            setMessage('Password reset successfully. You can sign in now.');
            setStep('request');
            setToken('');
            setNewPassword('');
            setConfirmPassword('');
        } catch (err: unknown) {
            setError(extractApiErrorMessage(err, 'Failed to reset password. Verify the token and try again.'));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-100 p-4">
            <div className="bg-white rounded-xl shadow-lg p-8 w-full max-w-md">
                <h1 className="text-2xl font-bold text-indigo-700 mb-6 text-center">Forgot Password</h1>

                {message && (
                    <p className="mb-4 text-sm text-green-700 bg-green-50 border border-green-200 rounded-md p-3">
                        {message}
                    </p>
                )}
                {error && (
                    <p className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                        {error}
                    </p>
                )}

                {step === 'request' ? (
                    <form onSubmit={handleRequestReset} className="space-y-4">
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
                        <button
                            type="submit"
                            disabled={loading || !email.trim()}
                            className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-medium py-2 rounded-md transition-colors"
                        >
                            {loading ? 'Generating…' : 'Generate Reset Token'}
                        </button>
                    </form>
                ) : (
                    <form onSubmit={handleResetPassword} className="space-y-4">
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
                            <label className="block text-sm font-medium text-slate-700 mb-1">Reset Token</label>
                            <textarea
                                value={token}
                                onChange={(e) => setToken(e.target.value)}
                                rows={3}
                                required
                                className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">New Password</label>
                            <input
                                type="password"
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                                required
                                minLength={8}
                                className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Confirm New Password</label>
                            <input
                                type="password"
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                required
                                minLength={8}
                                className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                        </div>
                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-medium py-2 rounded-md transition-colors"
                        >
                            {loading ? 'Resetting…' : 'Reset Password'}
                        </button>
                    </form>
                )}

                <p className="mt-4 text-center text-sm text-slate-500">
                    Back to{' '}
                    <Link to="/login" className="text-indigo-600 hover:underline">
                        Sign In
                    </Link>
                </p>
            </div>
        </div>
    );
}
