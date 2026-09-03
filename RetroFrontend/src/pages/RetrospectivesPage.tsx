import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { CreateRetroModal } from '../components/CreateRetroModal';
import { organizationsApi } from '../api/organizations';
import { retrospectivesApi } from '../api/retrospectives';
import { useAuthStore } from '../store/authStore';
import type { GetRetrospectiveSummaryDto, Organization } from '../types';

function OrganizationRetrospectives({ organization }: { organization: Organization }) {
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
    const grouped = (data?.items ?? []).reduce<Record<string, GetRetrospectiveSummaryDto[]>>((result, retro) => {
        (result[retro.title] ??= []).push(retro);
        return result;
    }, {});
    const titles = Object.keys(grouped).sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }));
    Object.values(grouped).forEach((items) => items.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)));

    return <section className="space-y-3">
        <div className="flex justify-between"><h2 className="text-xl font-semibold">{organization.name}</h2><span>{data?.totalCount ?? 0} boards</span></div>
        {isLoading && <p>Loading…</p>}
        {titles.map((title) => {
            const items = grouped[title];
            const open = expanded.has(title);
            return <div key={title} className="bg-white rounded-xl shadow border overflow-hidden">
                <button className="w-full p-4 flex justify-between" onClick={() => setExpanded((previous) => {
                    const next = new Set(previous); if (next.has(title)) next.delete(title); else next.add(title); return next;
                })}><span className="font-semibold">{open ? '▼' : '▶'} {title}</span><span>{items.length} session{items.length === 1 ? '' : 's'}</span></button>
                {open && items.map((retro) => <div key={retro.id} className="border-t p-3 flex justify-between">
                    <span>{new Date(retro.createdAt).toLocaleDateString()} · {retro.isClosed ? 'Closed' : 'Open'}</span>
                    <span className="space-x-3"><button className="text-indigo-600" onClick={() => navigate(`/retrospectives/${retro.id}`)}>Open</button>
                    {(role === 'Admin' || role === 'Manager') && <button className="text-red-600" onClick={() => {
                        if (confirm('Delete this retrospective?')) remove.mutate(retro.id);
                    }}>Delete</button>}</span>
                </div>)}
            </div>;
        })}
        <div className="flex justify-end gap-3">
            <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button><span>Page {page}</span>
            <button disabled={!data || page * data.pageSize >= data.totalCount} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
    </section>;
}

export function RetrospectivesPage() {
    const { role, organizationId } = useAuthStore();
    const [showCreate, setShowCreate] = useState(false);
    const [organizationPage, setOrganizationPage] = useState(1);
    const { data: organizations, isLoading } = useQuery({
        queryKey: ['organizations', organizationPage],
        queryFn: () => organizationsApi.getAll(organizationPage),
        enabled: role === 'Admin' || role === 'Manager',
    });
    const canManage = role === 'Admin' || role === 'Manager';

    return <Layout><div className="space-y-7">
        <div className="flex justify-between"><h1 className="text-2xl font-bold">Retrospectives</h1>
            {canManage && <button className="bg-indigo-600 text-white rounded px-4 py-2" onClick={() => setShowCreate(true)}>+ New Retrospective</button>}
        </div>
        {isLoading && <p>Loading organizations…</p>}
        {(organizations?.items ?? (organizationId ? [{ id: organizationId, name: 'Your organization' }] : []))
            .map((organization) => <OrganizationRetrospectives key={organization.id} organization={organization} />)}
        {role === 'Admin' && <div className="flex justify-center gap-4">
            <button disabled={organizationPage === 1} onClick={() => setOrganizationPage((p) => p - 1)}>Previous organizations</button>
            <span>Page {organizationPage}</span>
            <button disabled={!organizations || organizationPage * organizations.pageSize >= organizations.totalCount} onClick={() => setOrganizationPage((p) => p + 1)}>Next organizations</button>
        </div>}
        {showCreate && <CreateRetroModal onClose={() => setShowCreate(false)} />}
    </div></Layout>;
}
