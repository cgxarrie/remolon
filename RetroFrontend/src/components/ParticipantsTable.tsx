import { useEffect, useRef, useState } from 'react';

export interface AvatarEntry {
    id: string;
    name: string;
    subtitle?: string;
}

interface Props {
    currentUser: AvatarEntry;
    participants: AvatarEntry[];
    targetedIds?: string[];
    centerAction?: React.ReactNode;
    onParticipantClick?: (participantId: string) => void;
    currentUserRef?: (element: HTMLDivElement | null) => void;
    participantRef?: (participantId: string, element: HTMLDivElement | null) => void;
}

interface TableGeometry {
    radiusX: number;
    radiusY: number;
    width: number;
    height: number;
}

const CIRCLE_MAX_SEATS = 7;
const MIN_SEAT_SPACING = 74;
const MIN_TABLE_RADIUS = 120;
const MAX_TABLE_ASPECT = 1.9;
const SEAT_FOOTPRINT = 84;
const GRID_FALLBACK_WIDTH = 420;

// Seats are spread over equal angles, so the tightest gap between two neighbours is
// radiusY * (2π / seats) — that lower bound is what keeps avatars from overlapping.
function tableGeometry(seatCount: number): TableGeometry {
    const radiusY = Math.max(MIN_TABLE_RADIUS, (MIN_SEAT_SPACING * seatCount) / (2 * Math.PI));
    const aspect = seatCount > CIRCLE_MAX_SEATS
        ? Math.min(MAX_TABLE_ASPECT, 1.25 + (seatCount - CIRCLE_MAX_SEATS - 1) * 0.1)
        : 1;
    const radiusX = radiusY * aspect;

    return {
        radiusX,
        radiusY,
        width: radiusX * 2 + SEAT_FOOTPRINT,
        height: radiusY * 2 + SEAT_FOOTPRINT,
    };
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

export function ParticipantsTable({
    currentUser,
    participants,
    targetedIds = [],
    centerAction,
    onParticipantClick,
    currentUserRef,
    participantRef,
}: Props) {
    const containerRef = useRef<HTMLDivElement | null>(null);
    const [availableWidth, setAvailableWidth] = useState(0);

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        setAvailableWidth(container.clientWidth);

        const observer = new ResizeObserver((entries) => {
            setAvailableWidth(entries[0].contentRect.width);
        });
        observer.observe(container);

        return () => observer.disconnect();
    }, []);

    const geometry = tableGeometry(participants.length);
    const centerX = geometry.width / 2;
    const centerY = geometry.height / 2;
    // The table keeps its natural size and is only scaled down visually, so the seat
    // maths never has to account for the width the page happens to give it.
    const scale = availableWidth > 0 ? Math.min(1, availableWidth / geometry.width) : 1;
    const useGrid = availableWidth > 0 && availableWidth < GRID_FALLBACK_WIDTH;

    const currentUserSeat = (
        <>
            <div
                ref={currentUserRef}
                className="h-14 w-14 rounded-full flex items-center justify-center font-semibold bg-indigo-600 text-white border border-indigo-700 ring-4 ring-indigo-200"
                title={currentUser.name}
                aria-label={currentUser.name}
            >
                {initialsFrom(currentUser.name)}
            </div>
            <span className="px-2 py-0.5 rounded-full bg-indigo-600 text-white text-[10px] font-semibold tracking-wide uppercase">
                You
            </span>
            <p className="text-[11px] leading-tight text-slate-500 max-w-24 truncate" title={currentUser.name}>
                {currentUser.name}
            </p>
            {centerAction}
        </>
    );

    return (
        <div ref={containerRef} className="w-full">
            {useGrid ? (
                <div className="flex flex-col items-center gap-4">
                    <div className="flex flex-col items-center gap-1 text-center">
                        {currentUserSeat}
                    </div>
                    {participants.length === 0 ? (
                        <p className="text-xs text-slate-400 text-center">No other people yet</p>
                    ) : (
                        <div className="grid grid-cols-3 gap-3 justify-items-center w-full">
                            {participants.map((participant) => (
                                <AvatarPill
                                    key={participant.id}
                                    user={participant}
                                    isTargeted={targetedIds.includes(participant.id)}
                                    onClick={onParticipantClick && (() => onParticipantClick(participant.id))}
                                    avatarRef={participantRef && ((element: HTMLDivElement | null) => participantRef(participant.id, element))}
                                />
                            ))}
                        </div>
                    )}
                </div>
            ) : (
                <div className="relative overflow-hidden" style={{ height: geometry.height * scale }}>
                    <div
                        className="absolute left-1/2 top-0"
                        style={{
                            width: geometry.width,
                            height: geometry.height,
                            marginLeft: -centerX,
                            transform: `scale(${scale})`,
                            transformOrigin: 'top center',
                        }}
                    >
                        <div
                            className="absolute rounded-[50%] border border-slate-200 bg-slate-100/70 shadow-inner"
                            style={{
                                left: centerX - geometry.radiusX,
                                top: centerY - geometry.radiusY,
                                width: geometry.radiusX * 2,
                                height: geometry.radiusY * 2,
                            }}
                        />

                        <div
                            className="absolute flex flex-col items-center gap-1 text-center"
                            style={{ left: centerX, top: centerY, transform: 'translate(-50%, -50%)' }}
                        >
                            {currentUserSeat}
                            {participants.length === 0 && (
                                <p className="text-[11px] text-slate-400">No other people yet</p>
                            )}
                        </div>

                        {participants.map((participant, index) => {
                            const angle = (index / participants.length) * Math.PI * 2 - Math.PI / 2;

                            return (
                                <div
                                    key={participant.id}
                                    className="absolute"
                                    style={{
                                        left: centerX + geometry.radiusX * Math.cos(angle),
                                        top: centerY + geometry.radiusY * Math.sin(angle),
                                        transform: 'translate(-50%, -50%)',
                                    }}
                                >
                                    <AvatarPill
                                        user={participant}
                                        isTargeted={targetedIds.includes(participant.id)}
                                        onClick={onParticipantClick && (() => onParticipantClick(participant.id))}
                                        avatarRef={participantRef && ((element: HTMLDivElement | null) => participantRef(participant.id, element))}
                                    />
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}
        </div>
    );
}
