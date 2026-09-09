import { useEffect, useState } from 'react';
import { Layout } from '../components/Layout';
import { ProfileAvatarForm } from '../components/ProfileAvatarForm';
import { ProfilePasswordForm } from '../components/ProfilePasswordForm';
import { usersApi } from '../api/users';
import { useAuthStore } from '../store/authStore';

export function ProfilePage() {
    const nickname = useAuthStore((s) => s.nickname) ?? '';
    const email = useAuthStore((s) => s.email);
    const setAuth = useAuthStore((s) => s.setAuth);
    const setProfile = useAuthStore((s) => s.setProfile);
    const [draft, setDraft] = useState(nickname);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        setDraft(nickname);
    }, [nickname]);

    useEffect(() => {
        let cancelled = false;
        usersApi
            .getMe()
            .then((me) => {
                if (cancelled) return;
                setProfile({ nickname: me.nickname, avatarUrl: me.avatarUrl });
                setDraft(me.nickname);
            })
            .catch(() => {
                /* keep persisted session values */
            });
        return () => {
            cancelled = true;
        };
    }, [setProfile]);

    async function handleNickname(e: React.FormEvent) {
        e.preventDefault();
        setError('');
        setSuccess('');
        const next = draft.trim();
        if (!next) {
            setError('Nickname is required.');
            return;
        }

        setLoading(true);
        try {
            const res = await usersApi.updateMe({ nickname: next });
            setAuth(res);
            setSuccess('Nickname updated.');
        } catch (err: unknown) {
            const axiosErr = err as { response?: { data?: { message?: string } } };
            setError(axiosErr.response?.data?.message ?? 'Could not update nickname.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <Layout>
            <div className="max-w-xl mx-auto space-y-6">
                <h1 className="text-2xl font-bold">Profile</h1>
                {email && <p className="text-sm text-slate-500">{email}</p>}

                <section className="bg-white rounded-xl shadow p-4 space-y-3">
                    <h2 className="font-semibold">Nickname</h2>
                    {error && <p className="text-sm text-red-600">{error}</p>}
                    {success && <p className="text-sm text-emerald-700">{success}</p>}
                    <form onSubmit={handleNickname} className="space-y-3">
                        <input
                            className="w-full border rounded-md px-3 py-2 text-sm"
                            value={draft}
                            maxLength={50}
                            onChange={(e) => setDraft(e.target.value)}
                            aria-label="Nickname"
                        />
                        <button
                            type="submit"
                            className="theme-primary text-white rounded px-4 py-2 text-sm disabled:opacity-50"
                            disabled={loading}
                        >
                            {loading ? 'Saving…' : 'Save nickname'}
                        </button>
                    </form>
                </section>

                <section className="bg-white rounded-xl shadow p-4 space-y-3">
                    <h2 className="font-semibold">Password</h2>
                    <ProfilePasswordForm />
                </section>

                <section className="bg-white rounded-xl shadow p-4 space-y-3">
                    <h2 className="font-semibold">Avatar</h2>
                    <ProfileAvatarForm />
                </section>
            </div>
        </Layout>
    );
}
