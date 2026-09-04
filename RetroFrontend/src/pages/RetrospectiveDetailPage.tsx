import { useEffect, useMemo, useRef, useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
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

function getThrowableObject(id: ThrowableObject['id']): ThrowableObject {
    return THROWABLE_OBJECTS.find((obj) => obj.id === id) ?? THROWABLE_OBJECTS[0];
}

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
    const organizationId = useAuthStore((s) => s.organizationId);
    const selectedOrganizationId = useAuthStore((s) => s.selectedOrganizationId);
    const userId = useAuthStore((s) => s.userId);
    const email = useAuthStore((s) => s.email);
    const nickname = useAuthStore((s) => s.nickname);
    const canManage = role === 'Admin' || role === 'Manager';
    const activeOrganizationId = role === 'Admin' ? selectedOrganizationId : organizationId;
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
        onSuccess: (newRetroId) => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            queryClient.invalidateQueries({ queryKey: ['retrospective', activeOrganizationId, id] });
            navigate(`/retrospectives/${newRetroId}`);
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
            objectId: selectedThrowable.id,
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
                                                if (confirm('Close this retrospective? All items will be locked and a new session will be created.')) {
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
                    <div className="mb-4 p-3 bg-slate-100 border border-slate-200 rounded-lg text-sm text-slate-600">
                        This retrospective is <strong>closed</strong>. Items are read-only.
                    </div>
                )}

                <div className="flex items-start gap-4">
                    <aside className="w-32 flex-shrink-0 bg-white/75 backdrop-blur border border-slate-200 rounded-2xl p-3 flex flex-col items-center gap-2">
                        <p className="text-[11px] font-semibold tracking-wide text-slate-500 uppercase">You</p>
                        <AvatarPill
                            user={currentUser}
                            isCurrent
                            avatarRef={(element) => {
                                currentAvatarRef.current = element;
                            }}
                        />
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
                    </aside>

                    <div className="flex-1 min-w-0">
                        <RetroBoard retro={retro} assigneeOptions={assigneeOptions} />
                    </div>

                    <aside className="w-32 flex-shrink-0 bg-white/75 backdrop-blur border border-slate-200 rounded-2xl p-3">
                        <div className="mb-3 flex flex-col items-center gap-1">
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
                        <div className="flex flex-col items-center gap-3">
                            {otherUsers.length === 0 ? (
                                <p className="text-xs text-slate-400 text-center">No other people yet</p>
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
                    </aside>
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
