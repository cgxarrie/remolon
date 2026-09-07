import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { avatarsApi } from '../api/avatars';
import { useAuthStore } from '../store/authStore';
import { UserAvatar } from './UserAvatar';

export function ProfileAvatarForm() {
    const userId = useAuthStore((s) => s.userId);
    const nickname = useAuthStore((s) => s.nickname);
    const email = useAuthStore((s) => s.email);
    const avatarUrl = useAuthStore((s) => s.avatarUrl);
    const setProfile = useAuthStore((s) => s.setProfile);
    const queryClient = useQueryClient();
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);
    const displayName = nickname ?? email ?? '';
    const hasPhoto = Boolean(avatarUrl);

    async function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0];
        e.target.value = '';
        if (!file) return;

        setError('');
        setSuccess('');
        setLoading(true);
        try {
            const res = await avatarsApi.upload(file);
            setProfile({ avatarUrl: res.avatarUrl });
            await queryClient.invalidateQueries({ queryKey: ['users', 'me'] });
            setSuccess('Avatar updated.');
        } catch (err: unknown) {
            const axiosErr = err as { response?: { data?: { message?: string } } };
            setError(axiosErr.response?.data?.message ?? 'Could not upload avatar.');
        } finally {
            setLoading(false);
        }
    }

    async function handleRemove() {
        setError('');
        setSuccess('');
        setLoading(true);
        try {
            await avatarsApi.remove();
            setProfile({ avatarUrl: null });
            await queryClient.invalidateQueries({ queryKey: ['users', 'me'] });
            setSuccess('Avatar removed.');
        } catch {
            setError('Could not remove avatar.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="space-y-3">
            {error && <p className="text-sm text-red-600">{error}</p>}
            {success && <p className="text-sm text-emerald-700">{success}</p>}
            <div className="flex items-center gap-3">
                <UserAvatar
                    userId={userId}
                    avatarUrl={avatarUrl}
                    name={displayName}
                    className="w-16 h-16 text-lg border border-slate-200"
                />
            </div>
            <label className="block text-sm text-slate-700">
                Image (JPEG, PNG, or WebP, max 2 MB)
                <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    onChange={handleFile}
                    disabled={loading}
                    className="mt-1 block w-full text-sm"
                />
            </label>
            {hasPhoto && (
                <button
                    type="button"
                    onClick={handleRemove}
                    disabled={loading}
                    className="text-sm text-slate-600 underline disabled:opacity-50"
                >
                    Remove avatar
                </button>
            )}
        </div>
    );
}
