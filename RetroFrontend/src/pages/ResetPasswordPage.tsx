import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '../api/auth';
import { PasswordField, PasswordMatchStatus, getPasswordMatchState } from '../components/PasswordField';
import { useAuthStore } from '../store/authStore';
import { PASSWORD_MIN_LENGTH } from '../types';

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

function credentialsFromLocation(searchParams: URLSearchParams) {
    const hash = window.location.hash.startsWith('#')
        ? window.location.hash.slice(1)
        : '';
    const fromHash = new URLSearchParams(hash);
    const purpose = fromHash.get('purpose') ?? searchParams.get('purpose');
    return {
        email: (fromHash.get('email') ?? searchParams.get('email') ?? '').trim(),
        token: (fromHash.get('token') ?? searchParams.get('token') ?? '').trim(),
        isInvite: purpose === 'invite',
    };
}

export function ResetPasswordPage() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const setAuth = useAuthStore((s) => s.setAuth);
    const { email, token, isInvite } = credentialsFromLocation(searchParams);
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const linkValid = Boolean(email && token);
    const { passwordsMatch, passwordsMismatch } = getPasswordMatchState(newPassword, confirmPassword);

    async function handleResetPassword(e: React.FormEvent) {
        e.preventDefault();
        setError('');

        if (!linkValid) {
            setError('This reset link is invalid. Request a new one from the forgot password page.');
            return;
        }

        if (newPassword !== confirmPassword) {
            setError('New password and confirmation do not match.');
            return;
        }

        setLoading(true);
        try {
            const session = await authApi.resetPassword({
                email,
                token,
                newPassword,
            });
            setNewPassword('');
            setConfirmPassword('');
            setAuth(session);
            navigate('/retrospectives', { replace: true });
        } catch (err: unknown) {
            setError(extractApiErrorMessage(err, 'Failed to reset password. The link may be invalid or expired.'));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-100 p-4">
            <div className="bg-white rounded-xl shadow-lg p-8 w-full max-w-md">
                <h1 className="text-2xl font-bold text-indigo-700 mb-6 text-center">
                    {isInvite ? 'Set Password' : 'Reset Password'}
                </h1>

                {error && (
                    <p className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                        {error}
                    </p>
                )}

                {!linkValid ? (
                    <p className="text-sm text-slate-600">
                        This reset link is missing required information. Request a new email from{' '}
                        <Link to="/forgot-password" className="text-indigo-600 hover:underline">
                            Forgot Password
                        </Link>
                        .
                    </p>
                ) : (
                    <form onSubmit={handleResetPassword} className="space-y-4">
                        <p className="text-sm text-slate-600">
                            Choose a new password for <span className="font-medium">{email}</span>. This link expires{' '}
                            {isInvite ? '30 days' : '30 minutes'} after it was sent.
                        </p>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">New Password</label>
                            <PasswordField
                                value={newPassword}
                                onChange={setNewPassword}
                                required
                                minLength={PASSWORD_MIN_LENGTH}
                                autoComplete="new-password"
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">Confirm New Password</label>
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
                            {loading ? (isInvite ? 'Saving…' : 'Resetting…') : isInvite ? 'Set Password' : 'Reset Password'}
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
