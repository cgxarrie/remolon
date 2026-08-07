import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/auth';
import { useAuthStore } from '../store/authStore';
import type { PasswordChangeRequiredResponse } from '../types';

export function LoginPage() {
    const navigate = useNavigate();
    const setAuth = useAuthStore((s) => s.setAuth);
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [needsPasswordChange, setNeedsPasswordChange] = useState(false);
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setLoading(true);
        try {
            const res = await authApi.login({ email, password });
            setAuth(res.token, res.email, res.role, res.nickname);
            navigate('/');
        } catch (err: unknown) {
            const axiosErr = err as { response?: { status?: number; data?: PasswordChangeRequiredResponse } };
            if (axiosErr.response?.status === 403 && axiosErr.response.data?.requiresPasswordChange) {
                setNeedsPasswordChange(true);
                setError(axiosErr.response.data.message);
            } else {
                setError('Invalid email or password.');
            }
        } finally {
            setLoading(false);
        }
    }

    async function handleInitialPasswordChange(e: React.FormEvent) {
        e.preventDefault();
        if (!newPassword.trim()) {
            setError('New password is required.');
            return;
        }

        if (newPassword !== confirmPassword) {
            setError('New password and confirmation do not match.');
            return;
        }

        setError('');
        setLoading(true);
        try {
            const res = await authApi.changeInitialPassword({
                email,
                currentPassword: password,
                newPassword,
            });
            setAuth(res.token, res.email, res.role, res.nickname);
            navigate('/');
        } catch {
            setError('Could not change password. Ensure it meets policy requirements.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-100">
            <div className="bg-white rounded-xl shadow-lg p-8 w-full max-w-sm">
                <h1 className="text-2xl font-bold text-indigo-700 mb-6 text-center">Retro Molon</h1>
                <h2 className="text-lg font-semibold mb-4 text-center text-slate-700">
                    {needsPasswordChange ? 'Change Temporary Password' : 'Sign In'}
                </h2>
                {error && (
                    <p className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                        {error}
                    </p>
                )}
                {needsPasswordChange ? (
                    <form onSubmit={handleInitialPasswordChange} className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Email</label>
                            <input
                                type="email"
                                value={email}
                                disabled
                                className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm bg-slate-100"
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
                            {loading ? 'Updating…' : 'Update Password'}
                        </button>
                    </form>
                ) : (
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
                            <label className="block text-sm font-medium text-slate-700 mb-1">Password</label>
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                required
                                className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                        </div>
                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-medium py-2 rounded-md transition-colors"
                        >
                            {loading ? 'Signing in…' : 'Sign In'}
                        </button>
                    </form>
                )}
                <p className="mt-4 text-center text-sm text-slate-500">
                    Don't have an account?{' '}
                    <Link to="/register" className="text-indigo-600 hover:underline">
                        Register
                    </Link>
                </p>
            </div>
        </div>
    );
}
