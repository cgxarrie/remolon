import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';
import { Layout } from '../components/Layout';
import { RetroBoard } from '../components/RetroBoard';
import { AssignUserModal } from '../components/AssignUserModal';
import { useAuthStore } from '../store/authStore';

export function RetrospectiveDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const canManage = role === 'Admin' || role === 'Manager';

    const [editingTitle, setEditingTitle] = useState(false);
    const [titleValue, setTitleValue] = useState('');
    const [showAssign, setShowAssign] = useState(false);

    const { data: retro, isLoading, isError } = useQuery({
        queryKey: ['retrospective', id],
        queryFn: () => retrospectivesApi.getById(id!),
        enabled: !!id,
    });

    const closeMutation = useMutation({
        mutationFn: () => retrospectivesApi.close(id!),
        onSuccess: (newRetroId) => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            queryClient.invalidateQueries({ queryKey: ['retrospective', id] });
            navigate(`/retrospectives/${newRetroId}`);
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string }; status?: number } };
            if (axiosError.response?.status === 409) {
                alert('This retrospective is already closed.');
            }
        },
    });

    const deleteMutation = useMutation({
        mutationFn: () => retrospectivesApi.delete(id!),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            navigate('/');
        },
    });

    const setDateMutation = useMutation({
        mutationFn: () =>
            retrospectivesApi.update(id!, { retrospectiveDate: new Date().toISOString() }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospective', id] });
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
        },
    });

    const renameMutation = useMutation({
        mutationFn: (title: string) => retrospectivesApi.update(id!, { title }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospective', id] });
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            setEditingTitle(false);
        },
    });

    if (isLoading) {
        return (
            <Layout>
                <p className="text-slate-500">Loading retrospective…</p>
            </Layout>
        );
    }

    if (isError || !retro) {
        return (
            <Layout>
                <p className="text-red-600">Retrospective not found.</p>
            </Layout>
        );
    }

    return (
        <Layout>
            {/* Header */}
            <div className="flex flex-wrap items-start justify-between gap-4 mb-6">
                <div>
                    <button
                        onClick={() => navigate('/')}
                        className="text-sm text-indigo-600 hover:underline mb-1 inline-block"
                    >
                        ← Retrospectives
                    </button>
                    <div className="flex items-center gap-2">
                        {editingTitle ? (
                            <>
                                <input
                                    autoFocus
                                    value={titleValue}
                                    onChange={(e) => setTitleValue(e.target.value)}
                                    onKeyDown={(e) => {
                                        if (e.key === 'Enter') renameMutation.mutate(titleValue.trim());
                                        if (e.key === 'Escape') setEditingTitle(false);
                                    }}
                                    className="text-2xl font-bold text-slate-800 border-b-2 border-indigo-500 bg-transparent focus:outline-none w-72"
                                />
                                <button
                                    onClick={() => renameMutation.mutate(titleValue.trim())}
                                    disabled={renameMutation.isPending || !titleValue.trim()}
                                    className="text-green-600 hover:text-green-700 disabled:opacity-50 text-lg"
                                >
                                    ✓
                                </button>
                                <button
                                    onClick={() => setEditingTitle(false)}
                                    className="text-slate-400 hover:text-slate-600 text-lg"
                                >
                                    ✕
                                </button>
                            </>
                        ) : (
                            <>
                                <h1 className="text-2xl font-bold text-slate-800">{retro.title}</h1>
                                {canManage && !retro.isClosed && (
                                    <button
                                        onClick={() => { setTitleValue(retro.title); setEditingTitle(true); }}
                                        className="text-slate-400 hover:text-indigo-600 transition-colors text-base"
                                        title="Rename retrospective"
                                    >
                                        ✏️
                                    </button>
                                )}
                            </>
                        )}
                        {retro.isClosed ? (
                            <span className="px-2.5 py-0.5 text-xs font-semibold bg-slate-200 text-slate-600 rounded-full">
                                Closed
                            </span>
                        ) : (
                            <span className="px-2.5 py-0.5 text-xs font-semibold bg-green-100 text-green-700 rounded-full">
                                Open
                            </span>
                        )}
                    </div>
                    <p className="text-sm text-slate-400 mt-0.5">
                        Created {new Date(retro.createdAt).toLocaleDateString('en-US', {
                            year: 'numeric', month: 'long', day: 'numeric',
                        })}
                    </p>
                    {retro.retrospectiveDate && (
                        <p className="text-sm text-indigo-600 font-medium mt-0.5">
                            📅 {new Date(retro.retrospectiveDate).toLocaleDateString('en-US', {
                                year: 'numeric', month: 'long', day: 'numeric',
                            })}
                        </p>
                    )}
                </div>

                {canManage && (
                    <div className="flex flex-wrap gap-2">
                        <button
                            onClick={() => setShowAssign(true)}
                            className="px-3 py-2 text-sm bg-white border border-slate-300 hover:border-indigo-400 text-slate-700 rounded-lg transition-colors"
                        >
                            Manage Participants
                        </button>
                        {!retro.isClosed && (
                            <>
                                <button
                                    onClick={() => setDateMutation.mutate()}
                                    disabled={setDateMutation.isPending}
                                    className="px-3 py-2 text-sm bg-white border border-slate-300 hover:border-indigo-400 text-slate-700 rounded-lg transition-colors disabled:opacity-50"
                                    title="Set retrospective date to today"
                                >
                                    📅 Set Date to Today
                                </button>
                                <button
                                    onClick={() => {
                                        if (confirm('Close this retrospective? All items will be locked and a new session will be created.')) {
                                            closeMutation.mutate();
                                        }
                                    }}
                                    disabled={closeMutation.isPending}
                                    className="px-3 py-2 text-sm bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white rounded-lg transition-colors"
                                >
                                    {closeMutation.isPending ? 'Closing…' : 'Close Retrospective'}
                                </button>
                            </>
                        )}
                        <button
                            onClick={() => {
                                if (confirm('Delete this retrospective? This cannot be undone.')) {
                                    deleteMutation.mutate();
                                }
                            }}
                            disabled={deleteMutation.isPending}
                            className="px-3 py-2 text-sm bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white rounded-lg transition-colors"
                        >
                            Delete
                        </button>
                    </div>
                )}
            </div>

            {retro.isClosed && (
                <div className="mb-4 p-3 bg-slate-100 border border-slate-200 rounded-lg text-sm text-slate-600">
                    This retrospective is <strong>closed</strong>. Items are read-only.
                </div>
            )}

            <RetroBoard retro={retro} />

            {showAssign && <AssignUserModal retroId={retro.id} onClose={() => setShowAssign(false)} />}
        </Layout>
    );
}
