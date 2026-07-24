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

    const [pendingRoles, setPendingRoles] = useState<Record<string, Role>>({});
    const [saved, setSaved] = useState<Record<string, boolean>>({});

    if (currentRole !== 'Admin') return <Navigate to="/" replace />;

    const { data: users, isLoading, isError } = useQuery({
        queryKey: ['users'],
        queryFn: usersApi.getAll,
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

    function handleRoleChange(userId: string, newRole: Role) {
        setPendingRoles((p) => ({ ...p, [userId]: newRole }));
        setSaved((s) => { const n = { ...s }; delete n[userId]; return n; });
    }

    function handleSave(user: UserSummaryDto) {
        const role = pendingRoles[user.id];
        if (role) mutation.mutate({ id: user.id, role });
    }

    return (
        <Layout>
            <div className="max-w-4xl mx-auto">
                <h1 className="text-2xl font-bold text-slate-800 mb-6">User Management</h1>

                {isLoading && <p className="text-slate-500">Loading users…</p>}
                {isError && (
                    <p className="text-red-600 bg-red-50 border border-red-200 rounded-md p-3">
                        Failed to load users.
                    </p>
                )}

                {users && (
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
