import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { organizationsApi } from '../api/organizations';
import { useAuthStore } from '../store/authStore';

export function OrganizationsPage() {
    const role = useAuthStore((s) => s.role);
    const queryClient = useQueryClient();
    const [page, setPage] = useState(1);
    const [name, setName] = useState('');
    const [error, setError] = useState('');
    const { data } = useQuery({
        queryKey: ['organizations', page],
        queryFn: () => organizationsApi.getAll(page),
        enabled: role === 'Admin',
    });
    const refresh = () => queryClient.invalidateQueries({ queryKey: ['organizations'] });
    const mutation = useMutation({
        mutationFn: () => organizationsApi.create(name.trim()),
        onSuccess: () => { setName(''); setError(''); refresh(); },
        onError: (value: unknown) => {
            const e = value as { response?: { data?: { message?: string } } };
            setError(e.response?.data?.message ?? 'Unable to save organization.');
        },
    });
    if (role !== 'Admin') return <Navigate to="/" replace />;

    return <Layout><div className="max-w-4xl mx-auto space-y-5">
        <h1 className="text-2xl font-bold">Organizations</h1>
        <div className="bg-white rounded-xl shadow p-4 flex gap-3">
            <input className="border rounded p-2 flex-1" placeholder="Organization name" value={name} onChange={(e) => setName(e.target.value)} />
            <button className="bg-indigo-600 text-white rounded px-4" disabled={!name.trim()} onClick={() => mutation.mutate()}>Create</button>
        </div>
        {error && <p className="text-red-600">{error}</p>}
        <div className="bg-white rounded-xl shadow overflow-hidden">
            <table className="w-full"><thead><tr className="text-left border-b"><th className="p-3">Name</th><th /></tr></thead>
                <tbody>{data?.items.map((organization) => <tr key={organization.id} className="border-b">
                    <td className="p-3">{organization.name}</td>
                    <td className="p-3 text-right space-x-3">
                        <button className="text-indigo-600" onClick={async () => {
                            const nextName = prompt('Organization name', organization.name);
                            if (nextName?.trim()) { await organizationsApi.update(organization.id, nextName.trim()); refresh(); }
                        }}>Edit</button>
                        <button className="text-red-600" onClick={async () => {
                            if (confirm(`Delete ${organization.name}? All users, retrospectives, assignments, columns, and items in this organization will be permanently deleted.`)) {
                                await organizationsApi.delete(organization.id); refresh();
                            }
                        }}>Delete</button>
                    </td>
                </tr>)}</tbody>
            </table>
        </div>
        <div className="flex justify-center gap-4">
            <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
            <span>Page {page}</span>
            <button disabled={!data || page * data.pageSize >= data.totalCount} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
    </div></Layout>;
}
