import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { CreateRetroModal } from '../components/CreateRetroModal';
import { retrospectivesApi } from '../api/retrospectives';
import { useAuthStore } from '../store/authStore';
import type { GetRetrospectiveBoardDto, SelectedOrganization } from '../types';

// Boards and their sessions share the ['retrospectives'] prefix so that closing,
// deleting, or renaming a retrospective anywhere in the app invalidates both.
const boardsKey = (organizationId: string, page: number) =>
    ['retrospectives', organizationId, 'boards', page];

const sessionsKey = (organizationId: string, title: string, page: number) =>
    ['retrospectives', organizationId, 'sessions', title, page];

// Mounted only while a board is expanded, so reopening it always refetches.
function BoardSessions({ organizationId, title }: { organizationId: string; title: string }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const [page, setPage] = useState(1);
    const { data, isLoading } = useQuery({
        queryKey: sessionsKey(organizationId, title, page),
        queryFn: () => retrospectivesApi.getSessions(organizationId, title, page),
    });
    const remove = useMutation({
        mutationFn: retrospectivesApi.delete,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['retrospectives'] }),
    });

    if (isLoading && !data) return <p className="border-t p-3 text-sm text-slate-500">Loading sessions…</p>;

    return (
        <>
            {(data?.items ?? []).map((retro) => (
                <div key={retro.id} className="border-t p-3 flex justify-between items-center gap-3">
                    <button
                        type="button"
                        className="flex items-center gap-2 min-w-0 text-left"
                        onClick={() => navigate(`/retrospectives/${retro.id}`)}
                    >
                        <span className="theme-link">
                            {new Date(retro.createdAt).toLocaleDateString()}
                        </span>
                        {retro.isClosed ? (
                            <span className="px-2.5 py-0.5 text-xs font-semibold bg-slate-200 text-slate-600 rounded-full">
                                Closed
                            </span>
                        ) : (
                            <span className="px-2.5 py-0.5 text-xs font-semibold bg-green-100 text-green-700 rounded-full">
                                Open
                            </span>
                        )}
                    </button>
                    {role === 'Manager' && (
                        <button
                            type="button"
                            className="p-1.5 text-slate-400 hover:text-red-600 rounded-md transition-colors"
                            title="Delete retrospective"
                            aria-label="Delete retrospective"
                            onClick={() => {
                                if (confirm('Delete this retrospective?')) remove.mutate(retro.id);
                            }}
                        >
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="1.75"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                className="w-4 h-4"
                                aria-hidden="true"
                            >
                                <path d="M3 6h18" />
                                <path d="M8 6V4h8v2" />
                                <path d="M19 6l-1 14H6L5 6" />
                                <path d="M10 11v6" />
                                <path d="M14 11v6" />
                            </svg>
                        </button>
                    )}
                </div>
            ))}
            {data && data.totalCount > data.pageSize && (
                <div className="border-t p-3 flex justify-end gap-3 text-sm">
                    <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
                    <span>Page {page}</span>
                    <button
                        disabled={page * data.pageSize >= data.totalCount}
                        onClick={() => setPage((p) => p + 1)}
                    >
                        Next
                    </button>
                </div>
            )}
        </>
    );
}

function BoardRow({
    board,
    organizationId,
}: {
    board: GetRetrospectiveBoardDto;
    organizationId: string;
}) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const [expanded, setExpanded] = useState(false);
    const nextIteration = useMutation({
        mutationFn: (retroId: string) => retrospectivesApi.createNextIteration(retroId),
        onSuccess: (nextId) => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            navigate(`/retrospectives/${nextId}`);
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string } } };
            alert(axiosError.response?.data?.message ?? 'Could not start the next iteration.');
        },
    });

    return (
        <div className="bg-white rounded-xl shadow border overflow-hidden">
            <div
                role="button"
                tabIndex={0}
                className="w-full p-4 flex justify-between items-center gap-3 cursor-pointer"
                onClick={() => setExpanded((open) => !open)}
                onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        setExpanded((open) => !open);
                    }
                }}
            >
                <span className="flex items-center gap-2 min-w-0">
                    <span aria-hidden="true">{expanded ? '▼' : '▶'}</span>
                    {board.openSessionId ? (
                        <button
                            type="button"
                            className="font-semibold theme-link truncate text-left"
                            onClick={(event) => {
                                event.stopPropagation();
                                navigate(`/retrospectives/${board.openSessionId}`);
                            }}
                        >
                            {board.title}
                        </button>
                    ) : (
                        <span className="font-semibold truncate">{board.title}</span>
                    )}
                </span>
                <span className="flex-shrink-0 text-slate-500">
                    {board.sessionCount} session{board.sessionCount === 1 ? '' : 's'}
                </span>
            </div>
            {expanded && (
                <>
                    <BoardSessions organizationId={organizationId} title={board.title} />
                    {!board.openSessionId && board.latestClosedSessionId && role === 'Manager' && (
                        <div className="border-t p-3">
                            <button
                                className="theme-primary text-white rounded px-3 py-1.5 text-sm disabled:opacity-50"
                                disabled={nextIteration.isPending}
                                onClick={() => nextIteration.mutate(board.latestClosedSessionId!)}
                            >
                                {nextIteration.isPending ? 'Starting…' : 'Start next iteration'}
                            </button>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

function OrganizationRetrospectives({ organization }: { organization: SelectedOrganization }) {
    const [page, setPage] = useState(1);
    const { data, isLoading } = useQuery({
        queryKey: boardsKey(organization.id, page),
        queryFn: () => retrospectivesApi.getAll(organization.id, page),
    });

    return (
        <section className="space-y-3">
            <div className="flex justify-between">
                <h2 className="text-xl font-semibold">Boards</h2>
                <span>{data?.totalCount ?? 0} boards</span>
            </div>
            {isLoading && <p>Loading…</p>}
            {(data?.items ?? []).map((board) => (
                <BoardRow key={board.title} board={board} organizationId={organization.id} />
            ))}
            <div className="flex justify-end gap-3">
                <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
                <span>Page {page}</span>
                <button disabled={!data || page * data.pageSize >= data.totalCount} onClick={() => setPage((p) => p + 1)}>
                    Next
                </button>
            </div>
        </section>
    );
}

export function RetrospectivesPage() {
    const { role, organizationId, organizationName } = useAuthStore();
    const [showCreate, setShowCreate] = useState(false);
    const canManage = role === 'Manager';
    const activeOrganization: SelectedOrganization | null = organizationId
        ? {
            id: organizationId,
            name: organizationName ?? 'Your organization',
        }
        : null;

    return (
        <Layout>
            <div className="space-y-7">
                <div className="flex justify-between">
                    <h1 className="text-2xl font-bold">Retrospectives</h1>
                    {canManage && (
                        <button className="theme-primary text-white rounded px-4 py-2" onClick={() => setShowCreate(true)}>
                            + New Retrospective
                        </button>
                    )}
                </div>
                {activeOrganization && <OrganizationRetrospectives organization={activeOrganization} />}
                {showCreate && <CreateRetroModal onClose={() => setShowCreate(false)} />}
            </div>
        </Layout>
    );
}
