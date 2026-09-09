import { useEffect, useMemo, useRef, useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';
import { assignmentsApi } from '../api/assignments';
import { Layout } from '../components/Layout';
import { RetroBoard } from '../components/RetroBoard';
import { AssignUserModal } from '../components/AssignUserModal';
import { ParticipantsTable } from '../components/ParticipantsTable';
import type { AvatarEntry } from '../components/ParticipantsTable';
import { createRetrospectiveHubConnection } from '../api/retrospectiveHub';
import type { ItemsChangedEvent, ObjectThrownEvent, RetrospectiveAccessRevokedEvent, RetrospectiveClosedEvent, RetrospectiveDeletedEvent, RetrospectiveRevealedEvent } from '../api/retrospectiveHub';
import { invalidateRetrospective } from '../query/retrospectiveQueries';
import { useAuthStore } from '../store/authStore';

interface AxeFlightState {
    id: number;
    targetId: string;
    objectId: ThrowableObject['id'];
    startX: number;
    startY: number;
    endX: number;
    endY: number;
    flying: boolean;
}

interface ThrowableObject {
    id: 'axe' | 'hammer' | 'sword' | 'brick' | 'tomato' | 'shit';
    label: string;
    emoji: string;
    imageUrl?: string;
}

const AXE_FLIGHT_DURATION_MS = 2700;
const THROWABLE_OBJECTS: ThrowableObject[] = [
    { id: 'axe', label: 'axe', emoji: '🪓' },
    { id: 'hammer', label: 'hammer', emoji: '🔨' },
    { id: 'sword', label: 'sword', emoji: '🗡️' },
    { id: 'brick', label: 'brick', emoji: '🧱' },
    { id: 'tomato', label: 'tomato', emoji: '🍅' },
    { id: 'shit', label: 'shit', emoji: '💩' },
];

function getThrowableObject(id: string): ThrowableObject {
    return THROWABLE_OBJECTS.find((obj) => obj.id === id) ?? THROWABLE_OBJECTS[0];
}

function isThrowableId(id: string): id is ThrowableObject['id'] {
    return THROWABLE_OBJECTS.some((obj) => obj.id === id);
}

export function RetrospectiveDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((s) => s.role);
    const organizationId = useAuthStore((s) => s.organizationId);
    const userId = useAuthStore((s) => s.userId);
    const email = useAuthStore((s) => s.email);
    const nickname = useAuthStore((s) => s.nickname);
    const avatarUrl = useAuthStore((s) => s.avatarUrl);
    const canManage = role === 'Manager';
    const activeOrganizationId = organizationId;
    const throwablePreferenceKey = `retro-throwable:${userId ?? email ?? 'anonymous'}`;

    const [editingTitle, setEditingTitle] = useState(false);
    const [titleValue, setTitleValue] = useState('');
    const [showAssign, setShowAssign] = useState(false);
    const [axeFlights, setAxeFlights] = useState<AxeFlightState[]>([]);
    const [selectedThrowable, setSelectedThrowable] = useState<ThrowableObject>(THROWABLE_OBJECTS[0]);
    const [throwableMenu, setThrowableMenu] = useState<{ x: number; y: number; open: boolean }>({
        x: 0,
        y: 0,
        open: false,
    });

    const arenaRef = useRef<HTMLDivElement | null>(null);
    const currentAvatarRef = useRef<HTMLDivElement | null>(null);
    const targetAvatarRefs = useRef<Record<string, HTMLDivElement | null>>({});
    const clearFlightTimeoutRefs = useRef<number[]>([]);
    const nextFlightIdRef = useRef(0);
    const hubConnectionRef = useRef<ReturnType<typeof createRetrospectiveHubConnection> | null>(null);
    const playFlightRef = useRef<(
        fromUserId: string,
        targetId: string,
        objectId: ThrowableObject['id']
    ) => void>(() => undefined);

    const { data: retro, isLoading, isError } = useQuery({
        queryKey: ['retrospective', activeOrganizationId, id],
        queryFn: () => retrospectivesApi.getById(id!),
        enabled: Boolean(id && activeOrganizationId),
    });

    const { data: assignedParticipants = [] } = useQuery({
        queryKey: ['retrospectiveParticipants', id],
        queryFn: () => assignmentsApi.getRetrospectiveParticipants(id!),
        enabled: !!id,
    });

    const closeMutation = useMutation({
        mutationFn: () => retrospectivesApi.close(id!),
        onSuccess: (nextId) => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            if (nextId && nextId !== id) {
                navigate(`/retrospectives/${nextId}`);
                return;
            }
            queryClient.invalidateQueries({ queryKey: ['retrospective', activeOrganizationId, id] });
            queryClient.refetchQueries({ queryKey: ['retrospective', activeOrganizationId, id] });
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string }; status?: number } };
            if (axiosError.response?.status === 409) {
                alert(axiosError.response?.data?.message ?? 'This retrospective cannot be closed.');
            }
        },
    });

    const revealMutation = useMutation({
        mutationFn: () => retrospectivesApi.reveal(id!),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospective', activeOrganizationId, id] });
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            queryClient.refetchQueries({ queryKey: ['retrospective', activeOrganizationId, id] });
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { status?: number; data?: { message?: string } } };
            if (axiosError.response?.status === 403) {
                alert('You are not allowed to reveal this retrospective.');
                return;
            }

            alert(axiosError.response?.data?.message ?? 'Could not reveal retrospective.');
        },
    });

    const nextIterationMutation = useMutation({
        mutationFn: () => retrospectivesApi.createNextIteration(id!),
        onSuccess: (nextId) => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            navigate(`/retrospectives/${nextId}`);
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string }; status?: number } };
            alert(axiosError.response?.data?.message ?? 'Could not start the next iteration.');
        },
    });

    const deleteMutation = useMutation({
        mutationFn: () => retrospectivesApi.delete(id!),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            navigate('/retrospectives');
        },
    });

    const renameMutation = useMutation({
        mutationFn: (title: string) => retrospectivesApi.update(id!, { title }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospective', activeOrganizationId, id] });
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            setEditingTitle(false);
        },
    });

    const currentUserLabel = (nickname?.trim() || email?.trim() || 'You');
    const currentUser: AvatarEntry = {
        id: userId ?? email ?? 'current-user',
        name: currentUserLabel,
        avatarUrl,
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

        const add = (idValue: string, nameValue: string, avatarUrl?: string | null) => {
            const idKey = idValue.trim();
            const name = nameValue.trim();
            if (!idKey || !name || isCurrentUserValue(idKey) || isCurrentUserValue(name)) return;
            if (!map.has(idKey)) {
                map.set(idKey, { id: idKey, name, avatarUrl });
            }
        };

        assignedParticipants.forEach((participant) => {
            const displayName = participant.nickname?.trim() || 'Participant';
            add(participant.id || participant.email || displayName, displayName, participant.avatarUrl);
        });

        return Array.from(map.values()).sort((a, b) => a.name.localeCompare(b.name));
    }, [assignedParticipants, userId, email, nickname]);

    const assigneeOptions = useMemo(() => {
        const uniqueNames = new Set<string>();

        assignedParticipants.forEach((participant) => {
            const name = participant.nickname?.trim() || participant.email?.trim() || '';
            if (name) uniqueNames.add(name);
        });

        return Array.from(uniqueNames).sort((a, b) => a.localeCompare(b));
    }, [assignedParticipants]);

    useEffect(() => {
        return () => {
            clearFlightTimeoutRefs.current.forEach((timeoutId) => window.clearTimeout(timeoutId));
            clearFlightTimeoutRefs.current = [];
        };
    }, []);

    useEffect(() => {
        try {
            const stored = window.localStorage.getItem(throwablePreferenceKey) as ThrowableObject['id'] | null;
            if (!stored) {
                setSelectedThrowable(THROWABLE_OBJECTS[0]);
                return;
            }

            const restored = THROWABLE_OBJECTS.find((obj) => obj.id === stored) ?? THROWABLE_OBJECTS[0];
            setSelectedThrowable(restored);
        } catch {
            setSelectedThrowable(THROWABLE_OBJECTS[0]);
        }
    }, [throwablePreferenceKey]);

    useEffect(() => {
        try {
            window.localStorage.setItem(throwablePreferenceKey, selectedThrowable.id);
        } catch {
            // Ignore persistence errors (private mode, blocked storage, etc.)
        }
    }, [throwablePreferenceKey, selectedThrowable.id]);

    useEffect(() => {
        if (!throwableMenu.open) return;

        function handleWindowClick() {
            setThrowableMenu((prev) => ({ ...prev, open: false }));
        }

        function handleEscape(event: KeyboardEvent) {
            if (event.key === 'Escape') {
                setThrowableMenu((prev) => ({ ...prev, open: false }));
            }
        }

        window.addEventListener('click', handleWindowClick);
        window.addEventListener('keydown', handleEscape);

        return () => {
            window.removeEventListener('click', handleWindowClick);
            window.removeEventListener('keydown', handleEscape);
        };
    }, [throwableMenu.open]);

    function handleCurrentThrowableIconClick(event: React.MouseEvent<HTMLButtonElement>) {
        event.stopPropagation();

        const iconRect = event.currentTarget.getBoundingClientRect();

        if (throwableMenu.open) {
            setThrowableMenu((prev) => ({ ...prev, open: false }));
            return;
        }

        setThrowableMenu({
            x: iconRect.left + iconRect.width / 2,
            y: iconRect.bottom + 8,
            open: true,
        });
    }

    function resolveAvatar(personId: string): HTMLDivElement | null {
        const currentId = (userId ?? '').toLowerCase();
        if (personId.toLowerCase() === currentId) return currentAvatarRef.current;

        const direct = targetAvatarRefs.current[personId];
        if (direct) return direct;

        const match = Object.entries(targetAvatarRefs.current).find(
            ([id, element]) => Boolean(element) && id.toLowerCase() === personId.toLowerCase()
        );
        return match?.[1] ?? null;
    }

    function playFlight(fromUserId: string, targetId: string, objectId: ThrowableObject['id']) {
        const arena = arenaRef.current;
        const source = resolveAvatar(fromUserId);
        const target = resolveAvatar(targetId);

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
            objectId,
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

    playFlightRef.current = playFlight;

    useEffect(() => {
        if (!id || !userId) return;

        const connection = createRetrospectiveHubConnection();
        hubConnectionRef.current = connection;

        const handleThrown = (event: ObjectThrownEvent) => {
            if (!isThrowableId(event.objectId)) return;
            playFlightRef.current(event.fromUserId, event.targetUserId, event.objectId);
        };

        const refreshBoard = (event: ItemsChangedEvent | RetrospectiveRevealedEvent) => {
            if (event.retrospectiveId.toLowerCase() !== id.toLowerCase()) return;
            void invalidateRetrospective(queryClient, id);
            void queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            void queryClient.refetchQueries({
                predicate: (query) =>
                    query.queryKey[0] === 'retrospective' && query.queryKey.includes(id),
            });
        };

        const handleClosed = (event: RetrospectiveClosedEvent) => {
            refreshBoard(event);
        };

        const handleDeleted = (event: RetrospectiveDeletedEvent) => {
            if (event.retrospectiveId.toLowerCase() !== id.toLowerCase()) return;
            void queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            navigate('/retrospectives');
        };

        const handleAccessRevoked = (event: RetrospectiveAccessRevokedEvent) => {
            if (event.retrospectiveId.toLowerCase() !== id.toLowerCase()) return;
            void queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            navigate('/retrospectives');
        };

        connection.on('ObjectThrown', handleThrown);
        connection.on('ItemsChanged', refreshBoard);
        connection.on('RetrospectiveRevealed', refreshBoard);
        connection.on('RetrospectiveClosed', handleClosed);
        connection.on('RetrospectiveDeleted', handleDeleted);
        connection.on('RetrospectiveAccessRevoked', handleAccessRevoked);

        const join = () => connection.invoke('Join', id).catch(() => undefined);

        connection.onreconnected(() => {
            void join();
        });

        void connection.start().then(join).catch(() => undefined);

        return () => {
            connection.off('ObjectThrown', handleThrown);
            connection.off('ItemsChanged', refreshBoard);
            connection.off('RetrospectiveRevealed', refreshBoard);
            connection.off('RetrospectiveClosed', handleClosed);
            connection.off('RetrospectiveDeleted', handleDeleted);
            connection.off('RetrospectiveAccessRevoked', handleAccessRevoked);
            hubConnectionRef.current = null;
            void connection.stop();
        };
    }, [id, userId, queryClient, navigate]);

    function launchAxeToParticipant(targetId: string) {
        const fromUserId = userId ?? '';
        playFlight(fromUserId, targetId, selectedThrowable.id);
        void hubConnectionRef.current
            ?.invoke('Throw', id, targetId, selectedThrowable.id)
            .catch(() => undefined);
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

    if (retro.organizationId !== activeOrganizationId) {
        return <Navigate to="/retrospectives" replace />;
    }

    return (
        <Layout>
            <div ref={arenaRef} className="relative">
                {axeFlights.map((axeFlight) => (
                    <div key={axeFlight.id} className="pointer-events-none absolute inset-0 z-30">
                        {(() => {
                            const throwable = getThrowableObject(axeFlight.objectId);
                            return (
                                <div
                                    className="absolute select-none"
                                    style={{
                                        left: axeFlight.flying ? axeFlight.endX : axeFlight.startX,
                                        top: axeFlight.flying ? axeFlight.endY : axeFlight.startY,
                                        transform: 'translate(-50%, -50%)',
                                        transition: `left ${AXE_FLIGHT_DURATION_MS}ms cubic-bezier(0.22, 0.7, 0.2, 1), top ${AXE_FLIGHT_DURATION_MS}ms cubic-bezier(0.22, 0.7, 0.2, 1)`,
                                    }}
                                >
                                    <span className="inline-flex items-center justify-center h-8 w-8 animate-spin drop-shadow" style={{ animationDuration: '360ms' }}>
                                        {throwable.imageUrl ? (
                                            <img
                                                src={throwable.imageUrl}
                                                alt={throwable.label}
                                                className="h-7 w-7 object-contain"
                                            />
                                        ) : (
                                            <span className="text-2xl">{throwable.emoji}</span>
                                        )}
                                    </span>
                                </div>
                            );
                        })()}
                    </div>
                ))}

                {throwableMenu.open && (
                    <div
                        className="fixed z-50 bg-white border border-slate-200 rounded-lg shadow-lg p-1"
                        style={{ left: throwableMenu.x, top: throwableMenu.y, transform: 'translateX(-50%)' }}
                        onClick={(event) => event.stopPropagation()}
                    >
                        <div className="flex items-center gap-1">
                            {THROWABLE_OBJECTS.map((item) => (
                                <button
                                    key={item.id}
                                    onClick={() => {
                                        setSelectedThrowable(item);
                                        setThrowableMenu((prev) => ({ ...prev, open: false }));
                                    }}
                                    className={[
                                        'h-9 w-9 rounded-md text-xl flex items-center justify-center',
                                        selectedThrowable.id === item.id
                                            ? 'bg-indigo-50 text-indigo-700'
                                            : 'text-slate-700 hover:bg-slate-50',
                                    ].join(' ')}
                                    aria-label={item.label}
                                    title={item.label}
                                >
                                    {item.imageUrl ? (
                                        <img
                                            src={item.imageUrl}
                                            alt={item.label}
                                            className="h-6 w-6 object-contain"
                                        />
                                    ) : (
                                        <span>{item.emoji}</span>
                                    )}
                                </button>
                            ))}
                        </div>
                    </div>
                )}

                <div className="mb-6">
                    <button
                        onClick={() => navigate('/retrospectives')}
                        className="text-sm text-indigo-600 hover:underline mb-1 inline-block"
                    >
                        ← Retrospectives
                    </button>
                    <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
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
                            {canManage && (
                                <div className="flex items-center gap-2 mt-2">
                                    {!retro.isRevealed && (
                                        <button
                                            onClick={() => revealMutation.mutate()}
                                            disabled={revealMutation.isPending}
                                            className="p-2 bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white rounded-lg transition-colors"
                                            title={revealMutation.isPending ? 'Revealing…' : 'Reveal retrospective'}
                                            aria-label={revealMutation.isPending ? 'Revealing retrospective' : 'Reveal retrospective'}
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
                                                <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" />
                                                <circle cx="12" cy="12" r="3" />
                                            </svg>
                                        </button>
                                    )}
                                    {retro.isRevealed && !retro.isClosed && (
                                        <button
                                            onClick={() => {
                                                if (confirm('Close this retrospective? A new iteration will be created with pending and action items carried over as pending.')) {
                                                    closeMutation.mutate();
                                                }
                                            }}
                                            disabled={closeMutation.isPending}
                                            className="p-2 bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white rounded-lg transition-colors"
                                            title={closeMutation.isPending ? 'Closing…' : 'Close retrospective'}
                                            aria-label={closeMutation.isPending ? 'Closing retrospective' : 'Close retrospective'}
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
                                                <rect x="5" y="11" width="14" height="10" rx="2" />
                                                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                                            </svg>
                                        </button>
                                    )}
                                    <button
                                        onClick={() => {
                                            if (confirm('Delete this retrospective? This cannot be undone.')) {
                                                deleteMutation.mutate();
                                            }
                                        }}
                                        disabled={deleteMutation.isPending}
                                        className="p-2 bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white rounded-lg transition-colors"
                                        title={deleteMutation.isPending ? 'Deleting…' : 'Delete retrospective'}
                                        aria-label={deleteMutation.isPending ? 'Deleting retrospective' : 'Delete retrospective'}
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
                                </div>
                            )}
                            <p className="text-sm text-slate-400 mt-2">
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
                    </div>
                </div>

                {retro.isClosed && (
                    <div className="mb-4 p-3 bg-slate-100 border border-slate-200 rounded-lg text-sm text-slate-600 flex items-center justify-between gap-3">
                        <span>This retrospective is <strong>closed</strong>. Items are read-only.</span>
                        {canManage && retro.canStartNextIteration && (
                            <button
                                type="button"
                                onClick={() => nextIterationMutation.mutate()}
                                disabled={nextIterationMutation.isPending}
                                className="flex-shrink-0 px-3 py-1.5 theme-primary text-white text-xs font-medium rounded-md disabled:opacity-50"
                            >
                                {nextIterationMutation.isPending ? 'Starting…' : 'Start next iteration'}
                            </button>
                        )}
                    </div>
                )}

                <div className="space-y-4">
                    <section className="bg-white/75 backdrop-blur border border-slate-200 rounded-2xl p-4">
                        <div className="mb-2 flex items-center justify-between gap-2">
                            <p className="text-[11px] font-semibold tracking-wide text-slate-500 uppercase">
                                People
                            </p>
                            {canManage && (
                                <button
                                    type="button"
                                    onClick={() => setShowAssign(true)}
                                    className="p-1 text-slate-400 hover:text-indigo-600 rounded-md transition-colors"
                                    title="Manage participants"
                                    aria-label="Manage participants"
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
                                        <path d="M16 19v-1a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v1" />
                                        <circle cx="9" cy="7" r="4" />
                                        <path d="M19 8v6" />
                                        <path d="M22 11h-6" />
                                    </svg>
                                </button>
                            )}
                        </div>
                        <ParticipantsTable
                            currentUser={currentUser}
                            participants={otherUsers}
                            targetedIds={axeFlights.map((flight) => flight.targetId)}
                            onParticipantClick={launchAxeToParticipant}
                            currentUserRef={(element) => {
                                currentAvatarRef.current = element;
                            }}
                            participantRef={(participantId, element) => {
                                targetAvatarRefs.current[participantId] = element;
                            }}
                            centerAction={(
                                <button
                                    onClick={handleCurrentThrowableIconClick}
                                    className="text-lg leading-none rounded hover:bg-slate-100 p-1"
                                    aria-label={`Selected throwable ${selectedThrowable.label}`}
                                    title={selectedThrowable.label}
                                >
                                    {selectedThrowable.imageUrl ? (
                                        <img
                                            src={selectedThrowable.imageUrl}
                                            alt={selectedThrowable.label}
                                            className="h-6 w-6 object-contain"
                                        />
                                    ) : (
                                        selectedThrowable.emoji
                                    )}
                                </button>
                            )}
                        />
                    </section>

                    <RetroBoard retro={retro} assigneeOptions={assigneeOptions} />
                </div>
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
