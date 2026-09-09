import { useEffect, useRef, useState } from 'react';
import { UserAvatar } from './UserAvatar';

export interface AvatarEntry {
    id: string;
    name: string;
    subtitle?: string;
    avatarUrl?: string | null;
}

interface Props {
    currentUser: AvatarEntry;
    participants: AvatarEntry[];
    targetedIds?: string[];
    currentUserAction?: React.ReactNode;
    onParticipantClick?: (participantId: string) => void;
    currentUserRef?: (element: HTMLDivElement | null) => void;
    participantRef?: (participantId: string, element: HTMLDivElement | null) => void;
    children?: React.ReactNode;
}

type Edge = 'top' | 'bottom' | 'right' | 'left';

// Side stacks eat horizontal room, so they only appear once the board still has
// space to breathe next to them.
const SIDE_STACK_MIN_WIDTH = 1024;
const MAX_TOP = 4;
const MAX_BOTTOM = 6;
const MAX_SIDE = 3;

// People are dealt around the board one edge at a time so a small group still
// wraps the tickets instead of piling up on a single row.
function seatParticipants(participants: AvatarEntry[], allowSides: boolean): Record<Edge, AvatarEntry[]> {
    const rotation: Edge[] = allowSides ? ['top', 'bottom', 'right', 'left'] : ['top', 'bottom'];
    const limits: Record<Edge, number> = {
        top: MAX_TOP,
        bottom: MAX_BOTTOM,
        right: allowSides ? MAX_SIDE : 0,
        left: allowSides ? MAX_SIDE : 0,
    };
    const edges: Record<Edge, AvatarEntry[]> = { top: [], bottom: [], right: [], left: [] };

    let cursor = 0;
    participants.forEach((participant) => {
        let edge = rotation[cursor % rotation.length];

        for (let attempt = 0; attempt < rotation.length && edges[edge].length >= limits[edge]; attempt += 1) {
            cursor += 1;
            edge = rotation[cursor % rotation.length];
        }

        // Everyone that no longer fits joins the bottom row, which wraps.
        edges[edges[edge].length >= limits[edge] ? 'bottom' : edge].push(participant);
        cursor += 1;
    });

    return edges;
}

function AvatarPill({
    user,
    isCurrent = false,
    onClick,
    isTargeted = false,
    avatarRef,
    action,
}: {
    user: AvatarEntry;
    isCurrent?: boolean;
    onClick?: () => void;
    isTargeted?: boolean;
    avatarRef?: (element: HTMLDivElement | null) => void;
    action?: React.ReactNode;
}) {
    const clickable = Boolean(onClick);

    return (
        <div className={['flex flex-col items-center gap-1', isCurrent ? 'w-32' : 'w-24'].join(' ')}>
            <div
                ref={avatarRef}
                onClick={onClick}
                className={[
                    'h-12 w-12 flex-shrink-0 rounded-full overflow-hidden border transition-all',
                    isCurrent ? 'border-indigo-700 ring-4 ring-indigo-200' : 'border-white',
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
                <UserAvatar
                    userId={user.id}
                    avatarUrl={user.avatarUrl ?? null}
                    name={user.name}
                    className="h-12 w-12 text-sm border-0"
                    fallbackClassName={isCurrent ? 'bg-indigo-600 text-white' : undefined}
                />
            </div>
            <div className="flex max-w-full items-center gap-1">
                <p className="text-[11px] leading-tight text-slate-500 truncate" title={user.name}>
                    {user.name}
                </p>
                {action}
            </div>
            {user.subtitle && (
                <p className="text-[10px] leading-tight text-slate-400 max-w-full truncate" title={user.subtitle}>
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
    currentUserAction,
    onParticipantClick,
    currentUserRef,
    participantRef,
    children,
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

    const allowSides = availableWidth >= SIDE_STACK_MIN_WIDTH;
    const edges = seatParticipants(participants, allowSides);
    // The current user owns the middle of the top row, so their neighbours split
    // to either side of them.
    const topSplit = Math.ceil(edges.top.length / 2);
    const topLeft = edges.top.slice(0, topSplit);
    const topRight = edges.top.slice(topSplit);

    const renderParticipant = (participant: AvatarEntry) => (
        <AvatarPill
            key={participant.id}
            user={participant}
            isTargeted={targetedIds.includes(participant.id)}
            onClick={onParticipantClick && (() => onParticipantClick(participant.id))}
            avatarRef={participantRef && ((element: HTMLDivElement | null) => participantRef(participant.id, element))}
        />
    );

    return (
        <div ref={containerRef} className="w-full space-y-4">
            <div className="grid grid-cols-[1fr_auto_1fr] items-start gap-x-4">
                <div className="flex flex-wrap items-start justify-end gap-x-4 gap-y-3">
                    {topLeft.map(renderParticipant)}
                </div>
                <AvatarPill
                    user={currentUser}
                    isCurrent
                    avatarRef={currentUserRef}
                    action={currentUserAction}
                />
                <div className="flex flex-wrap items-start justify-start gap-x-4 gap-y-3">
                    {topRight.map(renderParticipant)}
                </div>
            </div>

            <div className="flex items-stretch gap-4">
                {edges.left.length > 0 && (
                    <div className="flex flex-col justify-center gap-4">
                        {edges.left.map(renderParticipant)}
                    </div>
                )}
                <div className="min-w-0 flex-1">{children}</div>
                {edges.right.length > 0 && (
                    <div className="flex flex-col justify-center gap-4">
                        {edges.right.map(renderParticipant)}
                    </div>
                )}
            </div>

            {edges.bottom.length > 0 && (
                <div className="flex flex-wrap items-start justify-center gap-x-4 gap-y-3">
                    {edges.bottom.map(renderParticipant)}
                </div>
            )}

            {participants.length === 0 && (
                <p className="text-xs text-slate-400 text-center">No other people yet</p>
            )}
        </div>
    );
}
