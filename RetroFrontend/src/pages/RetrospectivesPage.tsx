import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';
import { Layout } from '../components/Layout';
import { CreateRetroModal } from '../components/CreateRetroModal';
import { useAuthStore } from '../store/authStore';
import type { GetRetrospectiveSummaryDto } from '../types';

export function RetrospectivesPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const canManage = role === 'Admin' || role === 'Manager';

    const [showCreate, setShowCreate] = useState(false);
    const [expandedTitles, setExpandedTitles] = useState<Set<string>>(new Set());

    const { data: retros = [], isLoading, isError } = useQuery({
        queryKey: ['retrospectives'],
        queryFn: retrospectivesApi.getAll,
    });

    const deleteMutation = useMutation({
        mutationFn: retrospectivesApi.delete,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['retrospectives'] }),
    });

    // Group by title, sort each group by date descending
    const grouped = retros.reduce<Record<string, GetRetrospectiveSummaryDto[]>>((acc, retro) => {
        (acc[retro.title] ??= []).push(retro);
        return acc;
    }, {});

    Object.values(grouped).forEach((group) =>
        group.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    );

    const titles = Object.keys(grouped).sort();

    function toggleTitle(title: string) {
        setExpandedTitles((prev) => {
            const next = new Set(prev);
            if (next.has(title)) next.delete(title);
            else next.add(title);
            return next;
        });
    }

    function confirmDelete(id: string) {
        if (confirm('Delete this retrospective? This cannot be undone.')) {
            deleteMutation.mutate(id);
        }
    }

    return (
        <Layout>
            <div className="flex items-center justify-between mb-6">
                <h1 className="text-2xl font-bold text-slate-800">Retrospectives</h1>
                {canManage && (
                    <button
                        onClick={() => setShowCreate(true)}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg transition-colors"
                    >
                        + New Retrospective
                    </button>
                )}
            </div>

            {isLoading && <p className="text-slate-500">Loading…</p>}
            {isError && <p className="text-red-600">Failed to load retrospectives.</p>}

            {!isLoading && titles.length === 0 && (
                <div className="text-center py-16 text-slate-400">
                    <p className="text-4xl mb-3">📋</p>
                    <p className="text-lg font-medium">No retrospectives yet</p>
                    {canManage && (
                        <p className="text-sm mt-1">Create your first one to get started.</p>
                    )}
                </div>
            )}

            <div className="space-y-3">
                {titles.map((title) => {
                    const group = grouped[title];
                    const isExpanded = expandedTitles.has(title);
                    const allClosed = group.every((r) => r.isClosed);

                    return (
                        <div key={title} className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
                            <button
                                onClick={() => toggleTitle(title)}
                                className="w-full flex items-center justify-between px-5 py-4 text-left hover:bg-slate-50 transition-colors"
                            >
                                <div className="flex items-center gap-3">
                                    <span className="text-slate-400 text-sm">{isExpanded ? '▼' : '▶'}</span>
                                    <span className="font-semibold text-slate-800">{title}</span>
                                    <span className="text-xs text-slate-400 font-normal">
                                        {group.length} {group.length === 1 ? 'session' : 'sessions'}
                                    </span>
                                    {allClosed && (
                                        <span className="px-2 py-0.5 text-xs bg-slate-100 text-slate-500 rounded-full">
                                            All closed
                                        </span>
                                    )}
                                </div>
                                <span className="text-xs text-slate-400">
                                    Latest: {new Date(group[0].createdAt).toLocaleDateString()}
                                </span>
                            </button>

                            {isExpanded && (
                                <div className="border-t border-slate-100 divide-y divide-slate-100">
                                    {group.map((retro) => (
                                        <div key={retro.id} className="flex items-center justify-between px-5 py-3 hover:bg-slate-50">
                                            <div className="flex items-center gap-3">
                                                <div className="w-2 h-2 rounded-full flex-shrink-0" style={{ backgroundColor: retro.isClosed ? '#94a3b8' : '#6366f1' }} />
                                                <div>
                                                    <p className="text-sm font-medium text-slate-700">
                                                        {new Date(retro.createdAt).toLocaleDateString('en-US', {
                                                            year: 'numeric',
                                                            month: 'long',
                                                            day: 'numeric',
                                                        })}
                                                    </p>
                                                    <p className="text-xs text-slate-400">
                                                        {retro.isClosed ? 'Closed' : 'Open'} ·{' '}
                                                        Updated {new Date(retro.updatedAt).toLocaleDateString()}
                                                    </p>
                                                </div>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                {retro.isClosed && (
                                                    <span className="px-2 py-0.5 text-xs bg-slate-100 text-slate-500 rounded-full">
                                                        Closed
                                                    </span>
                                                )}
                                                <button
                                                    onClick={() => navigate(`/retrospectives/${retro.id}`)}
                                                    className="px-3 py-1.5 text-xs bg-indigo-50 hover:bg-indigo-100 text-indigo-700 rounded-md font-medium transition-colors"
                                                >
                                                    Open
                                                </button>
                                                {canManage && (
                                                    <button
                                                        onClick={() => confirmDelete(retro.id)}
                                                        className="px-3 py-1.5 text-xs bg-red-50 hover:bg-red-100 text-red-600 rounded-md transition-colors"
                                                    >
                                                        Delete
                                                    </button>
                                                )}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>

            {showCreate && <CreateRetroModal onClose={() => setShowCreate(false)} />}
        </Layout>
    );
}
