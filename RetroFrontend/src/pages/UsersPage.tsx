import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../api/users';
import { useAuthStore } from '../store/authStore';
import { Layout } from '../components/Layout';
import type { Role, UserSummaryDto } from '../types';

const ROLES: Role[] = ['Admin', 'Manager', 'StandardUser'];

export function UsersPage() {
    const { role: currentRole, userId: currentUserId } = useAuthStore();
    const queryClient = useQueryClient();
    const canCreateUsers = currentRole === 'Admin' || currentRole === 'Manager';
    const canManageUsers = currentRole === 'Admin';

    const [pendingRoles, setPendingRoles] = useState<Record<string, Role>>({});
    const [saved, setSaved] = useState<Record<string, boolean>>({});
    const [newEmail, setNewEmail] = useState('');
    const [newNickname, setNewNickname] = useState('');
    const [newRole, setNewRole] = useState<Role>('StandardUser');
    const [creationError, setCreationError] = useState('');
    const [createdCredentials, setCreatedCredentials] = useState<{ email: string; temporaryPassword: string } | null>(null);
    const [copiedPassword, setCopiedPassword] = useState(false);

    if (!canCreateUsers) return <Navigate to="/" replace />;

    const { data: users, isLoading, isError } = useQuery({
        queryKey: ['users'],
        queryFn: usersApi.getAll,
        enabled: canManageUsers,
    });

    const mutation = useMutation({
        mutationFn: ({ id, role }: { id: string; role: Role }) =>
            usersApi.updateRole(id, role),
        onSuccess: (updated) => {
            queryClient.setQueryData<UserSummaryDto[]>(['users'], (prev) =>
                prev?.map((u) => (u.id === updated.id ? updated : u)) ?? []
            );
            setSaved((s) => ({ ...s, [updated.id]: true }));
            setPendingRoles((p) => {
                const next = { ...p };
                delete next[updated.id];
                return next;
            });
            setTimeout(() => setSaved((s) => { const n = { ...s }; delete n[updated.id]; return n; }), 2000);
        },
    });

    const createUserMutation = useMutation({
        mutationFn: () =>
            usersApi.create({
                email: newEmail.trim(),
                nickname: newNickname.trim() || undefined,
                role: newRole,
            }),
        onSuccess: (created) => {
            if (canManageUsers) {
                queryClient.invalidateQueries({ queryKey: ['users'] });
            }
            setCreatedCredentials({ email: created.email, temporaryPassword: created.temporaryPassword });
            setCopiedPassword(false);
            setCreationError('');
            setNewEmail('');
            setNewNickname('');
            setNewRole('StandardUser');
        },
        onError: (err: unknown) => {
            const e = err as { response?: { data?: { message?: string } } };
            setCreationError(e.response?.data?.message ?? 'Failed to create user.');
            setCreatedCredentials(null);
        },
    });

    function handleRoleChange(userId: string, newRole: Role) {
        setPendingRoles((p) => ({ ...p, [userId]: newRole }));
        setSaved((s) => { const n = { ...s }; delete n[userId]; return n; });
    }

    function handleSave(user: UserSummaryDto) {
        const role = pendingRoles[user.id];
        if (role) mutation.mutate({ id: user.id, role });
    }

    async function handleCopyTemporaryPassword() {
        if (!createdCredentials?.temporaryPassword) return;
        try {
            await navigator.clipboard.writeText(createdCredentials.temporaryPassword);
            setCopiedPassword(true);
            setTimeout(() => setCopiedPassword(false), 2000);
        } catch {
            setCopiedPassword(false);
        }
    }

    return (
        <Layout>
            <div className="max-w-4xl mx-auto">
                <h1 className="text-2xl font-bold text-slate-800 mb-6">User Management</h1>

                <div className="mb-6 bg-white rounded-xl shadow p-4 border border-slate-200 space-y-3">
                    <h2 className="text-base font-semibold text-slate-700">Create User</h2>

                    {createdCredentials && (
                        <div className="text-sm text-amber-800 bg-amber-50 border border-amber-200 rounded-md p-3">
                            <p>
                                Created {createdCredentials.email}. Temporary password: <strong>{createdCredentials.temporaryPassword}</strong>
                            </p>
                            <div className="mt-2 flex items-center gap-2">
                                <button
                                    onClick={handleCopyTemporaryPassword}
                                    className="px-3 py-1 text-xs font-medium rounded-md bg-amber-600 hover:bg-amber-700 text-white"
                                >
                                    Copy Temporary Password
                                </button>
                                {copiedPassword && <span className="text-xs text-green-700 font-medium">Copied</span>}
                            </div>
                        </div>
                    )}

                    {creationError && (
                        <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                            {creationError}
                        </p>
                    )}

                    <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
                        <input
                            type="email"
                            placeholder="Email"
                            value={newEmail}
                            onChange={(e) => setNewEmail(e.target.value)}
                            className="md:col-span-2 border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                        <input
                            type="text"
                            placeholder="Nickname (optional)"
                            value={newNickname}
                            onChange={(e) => setNewNickname(e.target.value)}
                            className="border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                        <select
                            value={newRole}
                            onChange={(e) => setNewRole(e.target.value as Role)}
                            disabled={!canManageUsers}
                            className="border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        >
                            {(canManageUsers ? ROLES : (['StandardUser'] as Role[])).map((r) => (
                                <option key={r} value={r}>{r}</option>
                            ))}
                        </select>
                    </div>

                    <button
                        onClick={() => createUserMutation.mutate()}
                        disabled={createUserMutation.isPending || !newEmail.trim()}
                        className="px-4 py-2 text-sm font-medium rounded-md bg-indigo-600 hover:bg-indigo-700 text-white disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                        {createUserMutation.isPending ? 'Creating…' : 'Create User'}
                    </button>
                </div>

                {canManageUsers && isLoading && <p className="text-slate-500">Loading users…</p>}
                {canManageUsers && isError && (
                    <p className="text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                        Failed to load users.
                    </p>
                )}

                {canManageUsers && users && (
                    <div className="bg-white rounded-xl shadow overflow-hidden">
                        <table className="w-full text-sm">
                            <thead className="bg-slate-50 border-b border-slate-200">
                                <tr>
                                    <th className="text-left px-4 py-3 font-semibold text-slate-600">Email</th>
                                    <th className="text-left px-4 py-3 font-semibold text-slate-600">Nickname</th>
                                    <th className="text-left px-4 py-3 font-semibold text-slate-600">Role</th>
                                    <th className="px-4 py-3" />
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-100">
                                {users.map((user) => {
                                    const isSelf = user.id === currentUserId;
                                    const selectedRole = pendingRoles[user.id] ?? user.role;
                                    const isDirty = !!pendingRoles[user.id];
                                    const isSaving = mutation.isPending && mutation.variables?.id === user.id;

                                    return (
                                        <tr key={user.id} className="hover:bg-slate-50 transition-colors">
                                            <td className="px-4 py-3 text-slate-800">{user.email}</td>
                                            <td className="px-4 py-3 text-slate-500">{user.nickname || '—'}</td>
                                            <td className="px-4 py-3">
                                                <select
                                                    value={selectedRole}
                                                    disabled={isSelf}
                                                    onChange={(e) => handleRoleChange(user.id, e.target.value as Role)}
                                                    className="border border-slate-300 rounded-md px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-40 disabled:cursor-not-allowed"
                                                >
                                                    {ROLES.map((r) => (
                                                        <option key={r} value={r}>{r}</option>
                                                    ))}
                                                </select>
                                            </td>
                                            <td className="px-4 py-3 text-right">
                                                {isSelf ? (
                                                    <span className="text-xs text-slate-400">(you)</span>
                                                ) : saved[user.id] ? (
                                                    <span className="text-xs text-green-600 font-medium">Saved</span>
                                                ) : (
                                                    <button
                                                        disabled={!isDirty || isSaving}
                                                        onClick={() => handleSave(user)}
                                                        className="px-3 py-1 text-xs font-medium rounded-md bg-indigo-600 hover:bg-indigo-700 text-white disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                                                    >
                                                        {isSaving ? 'Saving…' : 'Save'}
                                                    </button>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </Layout>
    );
}
