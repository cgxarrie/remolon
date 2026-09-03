import { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';
import { organizationsApi } from '../api/organizations';
import { usersApi } from '../api/users';
import { useAuthStore } from '../store/authStore';
import { useQuery } from '@tanstack/react-query';

const DEFAULT_COLUMNS = ['What went well', 'What could be improved', 'What confused me'];

interface Props {
    onClose: () => void;
}

export function CreateRetroModal({ onClose }: Props) {
    const queryClient = useQueryClient();
    const [title, setTitle] = useState('');
    const [columns, setColumns] = useState<string[]>(DEFAULT_COLUMNS);
    const [newCol, setNewCol] = useState('');
    const [organizationId, setOrganizationId] = useState('');
    const [managerUserIds, setManagerUserIds] = useState<Set<string>>(new Set());
    const role = useAuthStore((s) => s.role);
    const ownOrganizationId = useAuthStore((s) => s.organizationId);
    const currentUserId = useAuthStore((s) => s.userId);
    const selectedOrganizationId = role === 'Admin' ? organizationId : ownOrganizationId ?? '';
    const { data: organizations } = useQuery({
        queryKey: ['organizations', 'create-retro'],
        queryFn: () => organizationsApi.getAll(1, 100),
        enabled: role === 'Admin',
    });
    const managersQuery = useQuery({
        queryKey: ['organizationManagers', selectedOrganizationId],
        queryFn: () => usersApi.getAll(selectedOrganizationId, 1, 100),
        enabled: Boolean(selectedOrganizationId),
        select: (page) => page.items.filter((user) => user.role === 'Manager'),
    });

    useEffect(() => {
        setManagerUserIds((current) => {
            const validIds = new Set(managersQuery.data?.map((manager) => manager.id) ?? []);
            const next = new Set([...current].filter((id) => validIds.has(id)));
            if (role === 'Manager' && currentUserId && validIds.has(currentUserId))
                next.add(currentUserId);
            return next;
        });
    }, [currentUserId, managersQuery.data, role, selectedOrganizationId]);

    const mutation = useMutation({
        mutationFn: () =>
            retrospectivesApi.create({
                title,
                organizationId: role === 'Admin' ? organizationId : undefined,
                managerUserIds: [...managerUserIds],
                columns: columns.map((c, i) => ({ title: c, position: i })),
            }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            onClose();
        },
    });

    function addColumn() {
        const trimmed = newCol.trim();
        if (trimmed) {
            setColumns((prev) => [...prev, trimmed]);
            setNewCol('');
        }
    }

    function removeColumn(i: number) {
        setColumns((prev) => prev.filter((_, idx) => idx !== i));
    }

    function toggleManager(userId: string) {
        setManagerUserIds((current) => {
            const next = new Set(current);
            if (next.has(userId)) next.delete(userId);
            else next.add(userId);
            return next;
        });
    }

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-xl w-full max-w-md">
                <div className="flex items-center justify-between px-6 py-4 border-b">
                    <h2 className="text-lg font-semibold">New Retrospective</h2>
                    <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">
                        ✕
                    </button>
                </div>
                <div className="p-6 space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Title</label>
                        <input
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="e.g. Sprint 42 Retrospective"
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    {role === 'Admin' && <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Organization</label>
                        <select
                            className="w-full border rounded-md px-3 py-2"
                            value={organizationId}
                            onChange={(e) => {
                                setOrganizationId(e.target.value);
                                setManagerUserIds(new Set());
                            }}
                        >
                            <option value="">Select organization</option>
                            {organizations?.items.map((organization) => <option key={organization.id} value={organization.id}>{organization.name}</option>)}
                        </select>
                    </div>}
                    {selectedOrganizationId && (
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-2">
                                Managers <span className="text-red-500">*</span>
                            </label>
                            <div className="border border-slate-200 rounded-lg max-h-40 overflow-y-auto divide-y divide-slate-100">
                                {managersQuery.isPending && (
                                    <p className="px-3 py-4 text-sm text-slate-500">Loading managers…</p>
                                )}
                                {managersQuery.isError && (
                                    <p className="px-3 py-4 text-sm text-red-600">Failed to load managers.</p>
                                )}
                                {!managersQuery.isPending && !managersQuery.isError && managersQuery.data?.length === 0 && (
                                    <p className="px-3 py-4 text-sm text-red-600">
                                        This organization has no managers.
                                    </p>
                                )}
                                {managersQuery.data?.map((manager) => (
                                    <label key={manager.id} className="flex items-center gap-3 px-3 py-2 hover:bg-indigo-50 cursor-pointer">
                                        <input
                                            type="checkbox"
                                            checked={managerUserIds.has(manager.id)}
                                            onChange={() => toggleManager(manager.id)}
                                            className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
                                        />
                                        <span className="min-w-0">
                                            <span className="block text-sm font-medium text-slate-800 truncate">{manager.nickname}</span>
                                            <span className="block text-xs text-slate-500 truncate">{manager.email}</span>
                                        </span>
                                    </label>
                                ))}
                            </div>
                            <p className="mt-1 text-xs text-slate-500">At least one manager must be assigned.</p>
                        </div>
                    )}
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Columns</label>
                        <ul className="space-y-2 mb-2">
                            {columns.map((col, i) => (
                                <li key={i} className="flex items-center gap-2">
                                    <span className="flex-1 text-sm bg-slate-50 border border-slate-200 rounded-md px-3 py-1.5">
                                        {col}
                                    </span>
                                    <button
                                        onClick={() => removeColumn(i)}
                                        className="text-red-400 hover:text-red-600 text-sm"
                                    >
                                        ✕
                                    </button>
                                </li>
                            ))}
                        </ul>
                        <div className="flex gap-2">
                            <input
                                type="text"
                                value={newCol}
                                onChange={(e) => setNewCol(e.target.value)}
                                onKeyDown={(e) => e.key === 'Enter' && addColumn()}
                                placeholder="Add column…"
                                className="flex-1 border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                            <button
                                onClick={addColumn}
                                className="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md text-sm"
                            >
                                Add
                            </button>
                        </div>
                    </div>
                    {mutation.isError && (
                        <p className="text-sm text-red-600">
                            {(mutation.error as { response?: { data?: { message?: string } } }).response?.data?.message
                                ?? 'Failed to create retrospective.'}
                        </p>
                    )}
                </div>
                <div className="flex justify-end gap-3 px-6 py-4 border-t">
                    <button
                        onClick={onClose}
                        className="px-4 py-2 text-sm text-slate-600 hover:text-slate-800"
                    >
                        Cancel
                    </button>
                    <button
                        onClick={() => mutation.mutate()}
                        disabled={!title.trim() || managerUserIds.size === 0 || mutation.isPending || managersQuery.isPending || (role === 'Admin' && !organizationId)}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium rounded-md transition-colors"
                    >
                        {mutation.isPending ? 'Creating…' : 'Create'}
                    </button>
                </div>
            </div>
        </div>
    );
}
