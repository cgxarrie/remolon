import { useState } from 'react';
import { authApi } from '../api/auth';
import { PasswordField, PasswordMatchStatus, getPasswordMatchState } from './PasswordField';
import { PASSWORD_MIN_LENGTH } from '../types';

export function ProfilePasswordForm() {
    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);
    const { passwordsMatch, passwordsMismatch } = getPasswordMatchState(newPassword, confirmPassword);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setSuccess('');

        if (!currentPassword || !newPassword) {
            setError('Current and new password are required.');
            return;
        }
        if (newPassword !== confirmPassword) {
            setError('New password and confirmation do not match.');
            return;
        }

        setLoading(true);
        try {
            await authApi.changePassword({ currentPassword, newPassword });
            setCurrentPassword('');
            setNewPassword('');
            setConfirmPassword('');
            setSuccess('Password updated.');
        } catch (err: unknown) {
            const axiosErr = err as { response?: { data?: { message?: string } | Array<{ description?: string }> } };
            const data = axiosErr.response?.data;
            if (Array.isArray(data)) {
                setError(data.map((item) => item.description).filter(Boolean).join(' ') || 'Could not change password.');
            } else {
                setError(data?.message ?? 'Could not change password.');
            }
        } finally {
            setLoading(false);
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-3">
            {error && <p className="text-sm text-red-600">{error}</p>}
            {success && <p className="text-sm text-emerald-700">{success}</p>}
            <label className="block text-sm text-slate-700">
                Current password
                <PasswordField
                    value={currentPassword}
                    onChange={setCurrentPassword}
                    toggleLabel="current password"
                    autoComplete="current-password"
                />
            </label>
            <label className="block text-sm text-slate-700">
                New password
                <PasswordField
                    value={newPassword}
                    onChange={setNewPassword}
                    toggleLabel="new password"
                    autoComplete="new-password"
                    minLength={PASSWORD_MIN_LENGTH}
                    matchState={passwordsMismatch ? 'mismatch' : passwordsMatch ? 'match' : 'none'}
                />
            </label>
            <label className="block text-sm text-slate-700">
                Confirm new password
                <PasswordField
                    value={confirmPassword}
                    onChange={setConfirmPassword}
                    toggleLabel="confirm password"
                    autoComplete="new-password"
                    matchState={passwordsMismatch ? 'mismatch' : passwordsMatch ? 'match' : 'none'}
                    aria-describedby="profile-password-match"
                />
                <PasswordMatchStatus
                    id="profile-password-match"
                    passwordsMatch={passwordsMatch}
                    confirmPassword={confirmPassword}
                />
            </label>
            <button
                type="submit"
                className="theme-primary text-white rounded px-4 py-2 text-sm disabled:opacity-50"
                disabled={loading || passwordsMismatch}
            >
                {loading ? 'Saving…' : 'Change password'}
            </button>
        </form>
    );
}
