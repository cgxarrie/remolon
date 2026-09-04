import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { CreateRetroModal } from '../components/CreateRetroModal';
import { retrospectivesApi } from '../api/retrospectives';
import { useAuthStore } from '../store/authStore';
import type { GetRetrospectiveSummaryDto, SelectedOrganization } from '../types';

function OrganizationRetrospectives({ organization }: { organization: SelectedOrganization }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const [page, setPage] = useState(1);
    const [expanded, setExpanded] = useState<Set<string>>(new Set());
    const { data, isLoading } = useQuery({
        queryKey: ['retrospectives', organization.id, page],
        queryFn: () => retrospectivesApi.getAll(organization.id, page),
    });
    const remove = useMutation({
        mutationFn: retrospectivesApi.delete,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['retrospectives', organization.id] }),
    });
    const nextIteration = useMutation({
        mutationFn: (retroId: string) => retrospectivesApi.createNextIteration(retroId),
        onSuccess: (nextId) => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives', organization.id] });
            navigate(`/retrospectives/${nextId}`);
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string } } };
            alert(axiosError.response?.data?.message ?? 'Could not start the next iteration.');
        },
    });
    const grouped = (data?.items ?? []).reduce<Record<string, GetRetrospectiveSummaryDto[]>>((result, retro) => {
        (result[retro.title] ??= []).push(retro);
        return result;
    }, {});
    const titles = Object.keys(grouped).sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }));
    Object.values(grouped).forEach((items) => items.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)));

    return <section className="space-y-3">
        <div className="flex justify-between"><h2 className="text-xl font-semibold">Boards</h2><span>{data?.totalCount ?? 0} boards</span></div>
        {isLoading && <p>Loading…</p>}
        {titles.map((title) => {
            const items = grouped[title];
            const open = expanded.has(title);
            const hasOpenIteration = items.some((retro) => !retro.isClosed);
            const latestClosed = items.find((retro) => retro.isClosed);
            return <div key={title} className="bg-white rounded-xl shadow border overflow-hidden">
                <button className="w-full p-4 flex justify-between" onClick={() => setExpanded((previous) => {
                    const next = new Set(previous); if (next.has(title)) next.delete(title); else next.add(title); return next;
                })}><span className="font-semibold">{open ? '▼' : '▶'} {title}</span><span>{items.length} session{items.length === 1 ? '' : 's'}</span></button>
                {open && items.map((retro) => <div key={retro.id} className="border-t p-3 flex justify-between">
                    <span>{new Date(retro.createdAt).toLocaleDateString()} · {retro.isClosed ? 'Closed' : 'Open'}</span>
                    <span className="space-x-3"><button className="theme-link" onClick={() => navigate(`/retrospectives/${retro.id}`)}>Open</button>
                    {(role === 'Admin' || role === 'Manager') && <button className="text-red-600" onClick={() => {
                        if (confirm('Delete this retrospective?')) remove.mutate(retro.id);
                    }}>Delete</button>}</span>
                </div>)}
                {open && !hasOpenIteration && latestClosed && (role === 'Admin' || role === 'Manager') && (
                    <div className="border-t p-3">
                        <button
                            className="theme-primary text-white rounded px-3 py-1.5 text-sm disabled:opacity-50"
                            disabled={nextIteration.isPending}
                            onClick={() => nextIteration.mutate(latestClosed.id)}
                        >
                            {nextIteration.isPending ? 'Starting…' : 'Start next iteration'}
                        </button>
                    </div>
                )}
            </div>;
        })}
        <div className="flex justify-end gap-3">
            <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button><span>Page {page}</span>
            <button disabled={!data || page * data.pageSize >= data.totalCount} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
    </section>;
}

export function RetrospectivesPage() {
    const { role, organizationId, selectedOrganizationId, selectedOrganizationName } = useAuthStore();
    const [showCreate, setShowCreate] = useState(false);
    const canManage = role === 'Admin' || role === 'Manager';
    const activeOrganizationId = role === 'Admin' ? selectedOrganizationId : organizationId;
    const activeOrganization: SelectedOrganization | null = activeOrganizationId
        ? {
            id: activeOrganizationId,
            name: role === 'Admin' ? selectedOrganizationName ?? 'Selected organization' : 'Your organization',
        }
        : null;

    return <Layout><div className="space-y-7">
        <div className="flex justify-between"><h1 className="text-2xl font-bold">Retrospectives</h1>
            {canManage && <button className="theme-primary text-white rounded px-4 py-2" onClick={() => setShowCreate(true)}>+ New Retrospective</button>}
        </div>
        {activeOrganization && <OrganizationRetrospectives organization={activeOrganization} />}
        {showCreate && <CreateRetroModal onClose={() => setShowCreate(false)} />}
    </div></Layout>;
}
