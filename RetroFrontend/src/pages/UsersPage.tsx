import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { usersApi } from '../api/users';
import { useAuthStore } from '../store/authStore';
import type { Organization, Role } from '../types';

function OrganizationUsers({ organization }: { organization: Organization }) {
    const [page, setPage] = useState(1);
    const queryClient = useQueryClient();
    const currentUserId = useAuthStore((s) => s.userId);
    const currentRole = useAuthStore((s) => s.role);
    const { data, isLoading } = useQuery({
        queryKey: ['users', organization.id, page],
        queryFn: () => usersApi.getAll(organization.id, page),
    });
    const remove = useMutation({
        mutationFn: usersApi.delete,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users', organization.id] }),
    });
    const updateRole = useMutation({
        mutationFn: ({ id, role }: { id: string; role: Role }) => usersApi.updateRole(id, role),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users', organization.id] }),
    });

    return (
        <section className="bg-white rounded-xl shadow border border-slate-200 overflow-hidden">
            <div className="px-4 py-3 bg-slate-50 border-b flex justify-between">
                <h2 className="font-semibold">Users</h2>
                <span className="text-sm text-slate-500">{data?.totalCount ?? 0} users</span>
            </div>
            {isLoading ? <p className="p-4 text-slate-500">Loading…</p> : (
                <table className="w-full text-sm">
                    <thead><tr className="text-left border-b"><th className="p-3">Nickname</th><th>Email</th><th>Role</th><th /></tr></thead>
                    <tbody>{data?.items.map((user) => (
                        <tr key={user.id} className="border-b last:border-0">
                            <td className="p-3">{user.nickname}</td><td>{user.email}</td><td>
                                {currentRole === 'Admin' && user.id !== currentUserId
                                    ? <select value={user.role} onChange={(e) => updateRole.mutate({ id: user.id, role: e.target.value as Role })}>
                                        <option>Admin</option><option>Manager</option><option>StandardUser</option>
                                    </select>
                                    : user.role}
                            </td>
                            <td className="text-right p-3">
                                {user.id !== currentUserId && <button className="text-red-600" onClick={() => {
                                    if (confirm(`Delete ${user.email}?`)) remove.mutate(user.id);
                                }}>Delete</button>}
                            </td>
                        </tr>
                    ))}</tbody>
                </table>
            )}
            <div className="p-3 border-t flex justify-end gap-3 text-sm">
                <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
                <span>Page {page}</span>
                <button disabled={!data || page * data.pageSize >= data.totalCount} onClick={() => setPage((p) => p + 1)}>Next</button>
            </div>
        </section>
    );
}

export function UsersPage() {
    const { role, organizationId, selectedOrganizationId, selectedOrganizationName } = useAuthStore();
    const queryClient = useQueryClient();
    const [email, setEmail] = useState('');
    const [nickname, setNickname] = useState('');
    const [newRole, setNewRole] = useState<Role>('StandardUser');
    const [error, setError] = useState('');

    const canView = role === 'Admin' || role === 'Manager';
    const activeOrganizationId = role === 'Admin' ? selectedOrganizationId : organizationId;
    const activeOrganization: Organization | null = activeOrganizationId
        ? {
            id: activeOrganizationId,
            name: role === 'Admin' ? selectedOrganizationName ?? 'Selected organization' : 'Your organization',
        }
        : null;

    const create = useMutation({
        mutationFn: () => usersApi.create({
            email: email.trim(),
            nickname: nickname.trim() || undefined,
            role: role === 'Manager' ? 'StandardUser' : newRole,
            organizationId: role === 'Admin' && newRole !== 'Admin'
                ? activeOrganizationId ?? undefined
                : undefined,
        }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
            setEmail(''); setNickname(''); setError('');
        },
        onError: (value: unknown) => {
            const e = value as { response?: { data?: { message?: string } } };
            setError(e.response?.data?.message ?? 'Failed to create user.');
        },
    });

    if (!canView) return <Navigate to="/" replace />;

    return <Layout><div className="max-w-5xl mx-auto space-y-6">
        <h1 className="text-2xl font-bold">User Management</h1>
        <div className="bg-white rounded-xl shadow p-4 space-y-3">
            <h2 className="font-semibold">Create User</h2>
            {error && <p className="text-red-600">{error}</p>}
            <div className="grid md:grid-cols-4 gap-3">
                <input className="border rounded p-2" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
                <input className="border rounded p-2" placeholder="Nickname" value={nickname} onChange={(e) => setNickname(e.target.value)} />
                {role === 'Admin' && <select className="border rounded p-2" value={newRole} onChange={(e) => setNewRole(e.target.value as Role)}>
                    <option>Admin</option><option>Manager</option><option>StandardUser</option>
                </select>}
            </div>
            <button className="theme-primary text-white rounded px-4 py-2 disabled:opacity-50" disabled={!email || create.isPending || (role === 'Admin' && newRole !== 'Admin' && !activeOrganizationId)} onClick={() => create.mutate()}>Create User</button>
        </div>
        {activeOrganization && <OrganizationUsers organization={activeOrganization} />}
    </div></Layout>;
}
