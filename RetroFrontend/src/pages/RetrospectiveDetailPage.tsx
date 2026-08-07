import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';
import { assignmentsApi } from '../api/assignments';
import { Layout } from '../components/Layout';
import { RetroBoard } from '../components/RetroBoard';
import { AssignUserModal } from '../components/AssignUserModal';
import { useAuthStore } from '../store/authStore';

interface AvatarEntry {
    id: string;
    name: string;
    subtitle?: string;
}

interface AxeFlightState {
    id: number;
    targetId: string;
    startX: number;
    startY: number;
    endX: number;
    endY: number;
    flying: boolean;
}

const AXE_FLIGHT_DURATION_MS = 1800;

function initialsFrom(name: string): string {
    const parts = name
        .trim()
        .split(/\s+/)
        .filter(Boolean);

    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function colorClassFrom(id: string): string {
    const classes = [
        'bg-rose-100 text-rose-700',
        'bg-amber-100 text-amber-700',
        'bg-emerald-100 text-emerald-700',
        'bg-cyan-100 text-cyan-700',
        'bg-indigo-100 text-indigo-700',
    ];

    let sum = 0;
    for (let i = 0; i < id.length; i += 1) sum += id.charCodeAt(i);
    return classes[sum % classes.length];
}

function AvatarPill({
    user,
    isCurrent = false,
    onClick,
    isTargeted = false,
    avatarRef,
}: {
    user: AvatarEntry;
    isCurrent?: boolean;
    onClick?: () => void;
    isTargeted?: boolean;
    avatarRef?: (element: HTMLDivElement | null) => void;
}) {
    const clickable = Boolean(onClick);

    return (
        <div className="flex flex-col items-center gap-1 text-center">
            <div
                ref={avatarRef}
                onClick={onClick}
                className={[
                    'h-12 w-12 rounded-full flex items-center justify-center font-semibold text-sm border transition-all',
                    isCurrent ? 'bg-indigo-600 text-white border-indigo-700' : `${colorClassFrom(user.id)} border-white`,
                    clickable ? 'cursor-pointer hover:scale-105 hover:shadow-md' : '',
                    isTargeted ? 'ring-4 ring-amber-300 scale-105' : '',
                ].join(' ')}
                title={user.name}
                aria-label={user.name}
                role={clickable ? 'button' : undefined}
                tabIndex={clickable ? 0 : -1}
                onKeyDown={(event) => {
                    if (!clickable) return;
                    if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        onClick?.();
                    }
                }}
            >
                {initialsFrom(user.name)}
            </div>
            <p className="text-[11px] leading-tight text-slate-500 max-w-16 truncate" title={user.name}>
                {user.name}
            </p>
            {user.subtitle && (
                <p className="text-[10px] leading-tight text-slate-400 max-w-16 truncate" title={user.subtitle}>
                    {user.subtitle}
                </p>
            )}
        </div>
    );
}

export function RetrospectiveDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const userId = useAuthStore((s) => s.userId);
    const email = useAuthStore((s) => s.email);
    const nickname = useAuthStore((s) => s.nickname);
    const canManage = role === 'Admin' || role === 'Manager';

    const [editingTitle, setEditingTitle] = useState(false);
    const [titleValue, setTitleValue] = useState('');
    const [showAssign, setShowAssign] = useState(false);
    const [axeFlights, setAxeFlights] = useState<AxeFlightState[]>([]);

    const arenaRef = useRef<HTMLDivElement | null>(null);
    const currentAvatarRef = useRef<HTMLDivElement | null>(null);
    const targetAvatarRefs = useRef<Record<string, HTMLDivElement | null>>({});
    const clearFlightTimeoutRefs = useRef<number[]>([]);
    const nextFlightIdRef = useRef(0);

    const { data: retro, isLoading, isError } = useQuery({
        queryKey: ['retrospective', id],
        queryFn: () => retrospectivesApi.getById(id!),
        enabled: !!id,
    });

    const { data: assignedParticipants = [] } = useQuery({
        queryKey: ['retrospectiveParticipants', id],
        queryFn: () => assignmentsApi.getRetrospectiveParticipants(id!),
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

    const currentUserLabel = (nickname?.trim() || 'You');
    const currentUser: AvatarEntry = {
        id: userId ?? email ?? 'current-user',
        name: currentUserLabel,
    };

    const otherUsers = useMemo(() => {
        const map = new Map<string, AvatarEntry>();
        const currentId = (userId ?? '').toLowerCase();
        const currentEmail = (email ?? '').toLowerCase();
        const currentNickname = (nickname ?? '').toLowerCase();

        const isCurrentUserValue = (value: string) => {
            const v = value.toLowerCase();
            return Boolean(v) && (v === currentId || v === currentEmail || v === currentNickname);
        };

        const add = (idValue: string, nameValue: string, subtitle?: string) => {
            const idKey = idValue.trim();
            const name = nameValue.trim();
            if (!idKey || !name || isCurrentUserValue(idKey) || isCurrentUserValue(name)) return;
            if (!map.has(idKey)) {
                map.set(idKey, { id: idKey, name, subtitle });
            }
        };

        assignedParticipants.forEach((participant) => {
            const displayName = participant.nickname?.trim() || 'Participant';
            add(participant.id || participant.email || displayName, displayName);
        });

        return Array.from(map.values()).sort((a, b) => a.name.localeCompare(b.name));
    }, [assignedParticipants, userId, email, nickname]);

    useEffect(() => {
        return () => {
            clearFlightTimeoutRefs.current.forEach((timeoutId) => window.clearTimeout(timeoutId));
            clearFlightTimeoutRefs.current = [];
        };
    }, []);

    function launchAxeToParticipant(targetId: string) {
        const arena = arenaRef.current;
        const source = currentAvatarRef.current;
        const target = targetAvatarRefs.current[targetId];

        if (!arena || !source || !target) return;

        const arenaRect = arena.getBoundingClientRect();
        const sourceRect = source.getBoundingClientRect();
        const targetRect = target.getBoundingClientRect();

        const startX = sourceRect.left + sourceRect.width / 2 - arenaRect.left;
        const startY = sourceRect.top + sourceRect.height / 2 - arenaRect.top;
        const endX = targetRect.left + targetRect.width / 2 - arenaRect.left;
        const endY = targetRect.top + targetRect.height / 2 - arenaRect.top;

        nextFlightIdRef.current += 1;
        const flightId = nextFlightIdRef.current;

        setAxeFlights((prev) => ([...prev, {
            id: flightId,
            targetId,
            startX,
            startY,
            endX,
            endY,
            flying: false,
        }]));

        requestAnimationFrame(() => {
            setAxeFlights((prev) => prev.map((flight) => (
                flight.id === flightId ? { ...flight, flying: true } : flight
            )));
        });

        const timeoutId = window.setTimeout(() => {
            setAxeFlights((prev) => prev.filter((flight) => flight.id !== flightId));
            clearFlightTimeoutRefs.current = clearFlightTimeoutRefs.current.filter((id) => id !== timeoutId);
        }, AXE_FLIGHT_DURATION_MS);

        clearFlightTimeoutRefs.current.push(timeoutId);
    }

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
            <div ref={arenaRef} className="relative grid grid-cols-1 xl:grid-cols-[5rem_minmax(0,1fr)_5rem] gap-5 xl:gap-8 items-start">
                {axeFlights.map((axeFlight) => (
                    <div key={axeFlight.id} className="pointer-events-none absolute inset-0 z-30">
                        <div
                            className="absolute select-none"
                            style={{
                                left: axeFlight.flying ? axeFlight.endX : axeFlight.startX,
                                top: axeFlight.flying ? axeFlight.endY : axeFlight.startY,
                                transform: 'translate(-50%, -50%)',
                                transition: `left ${AXE_FLIGHT_DURATION_MS}ms cubic-bezier(0.22, 0.7, 0.2, 1), top ${AXE_FLIGHT_DURATION_MS}ms cubic-bezier(0.22, 0.7, 0.2, 1)`,
                            }}
                        >
                            <span className="inline-block text-2xl animate-spin drop-shadow" style={{ animationDuration: '180ms' }}>
                                🪓
                            </span>
                        </div>
                    </div>
                ))}

                <aside className="order-2 xl:order-1 xl:sticky xl:top-24">
                    <div className="bg-white/75 backdrop-blur border border-slate-200 rounded-2xl p-3 flex xl:flex-col items-center gap-3">
                        <p className="text-[11px] font-semibold tracking-wide text-slate-500 uppercase">You</p>
                        <AvatarPill
                            user={currentUser}
                            isCurrent
                            avatarRef={(element) => {
                                currentAvatarRef.current = element;
                            }}
                        />
                    </div>
                </aside>

                <section className="order-1 xl:order-2 min-w-0">
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
                </section>

                <aside className="order-3 xl:sticky xl:top-24">
                    <div className="bg-white/75 backdrop-blur border border-slate-200 rounded-2xl p-3">
                        <p className="text-[11px] font-semibold tracking-wide text-slate-500 uppercase mb-3 text-center">
                            Players
                        </p>
                        <div className="flex xl:flex-col flex-wrap justify-center gap-3">
                            {otherUsers.length === 0 ? (
                                <p className="text-xs text-slate-400 text-center">No other players yet</p>
                            ) : (
                                otherUsers.map((user) => (
                                    <AvatarPill
                                        key={user.id}
                                        user={user}
                                        isTargeted={axeFlights.some((flight) => flight.targetId === user.id)}
                                        onClick={() => launchAxeToParticipant(user.id)}
                                        avatarRef={(element) => {
                                            targetAvatarRefs.current[user.id] = element;
                                        }}
                                    />
                                ))
                            )}
                        </div>
                    </div>
                </aside>
            </div>

            {showAssign && (
                <AssignUserModal
                    retroId={retro.id}
                    onClose={() => {
                        setShowAssign(false);
                        queryClient.invalidateQueries({ queryKey: ['retrospectiveParticipants', retro.id] });
                    }}
                />
            )}
        </Layout>
    );
}
