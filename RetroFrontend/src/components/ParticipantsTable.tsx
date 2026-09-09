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
    centerAction?: React.ReactNode;
    onParticipantClick?: (participantId: string) => void;
    currentUserRef?: (element: HTMLDivElement | null) => void;
    participantRef?: (participantId: string, element: HTMLDivElement | null) => void;
}

interface Seat {
    left: number;
    top: number;
    transform: string;
}

interface TableLayout {
    width: number;
    height: number;
    tableLeft: number;
    tableWidth: number;
    tableTop: number;
    seats: Seat[];
}

const SEAT_WIDTH = 148;
const SEAT_HEIGHT = 56;
// Seats hang over the table edge so the block stays as short as the avatars allow.
const SEAT_OVERLAP = 6;
const MAX_SEAT_SPACING = 168;
const TABLE_HEIGHT = 80;
const TABLE_MIN_WIDTH = 280;
const TABLE_MAX_WIDTH = 1040;
const GRID_FALLBACK_WIDTH = 420;

function rowPositions(count: number, width: number): number[] {
    if (count === 0) return [];
    if (count === 1) return [width / 2];

    const spacing = Math.min(MAX_SEAT_SPACING, (width - SEAT_WIDTH) / (count - 1));
    const start = (width - spacing * (count - 1)) / 2;

    return Array.from({ length: count }, (_, index) => start + spacing * index);
}

// Seats are handed out around the perimeter — long edges first, then the short
// sides — and returned clockwise starting at the top-left.
function tableLayout(seatCount: number, availableWidth: number): TableLayout {
    const outerWidth = Math.min(TABLE_MAX_WIDTH, Math.max(TABLE_MIN_WIDTH, availableWidth));
    // The short edges only hold one seat each, and only if the table still has room to breathe.
    const sideCount = outerWidth >= TABLE_MIN_WIDTH + 2 * SEAT_WIDTH
        ? Math.min(2, Math.max(0, seatCount - 2))
        : 0;
    const rightCount = sideCount >= 1 ? 1 : 0;
    const leftCount = sideCount >= 2 ? 1 : 0;
    const rowCount = seatCount - rightCount - leftCount;
    const topCount = Math.ceil(rowCount / 2);
    const bottomCount = rowCount - topCount;

    const tableLeft = sideCount > 0 ? SEAT_WIDTH : 0;
    const tableWidth = Math.min(
        outerWidth - 2 * tableLeft,
        Math.max(TABLE_MIN_WIDTH, topCount * SEAT_WIDTH),
    );
    const rowHeight = SEAT_HEIGHT - SEAT_OVERLAP;
    const tableTop = topCount > 0 ? rowHeight : 0;
    const tableBottom = tableTop + TABLE_HEIGHT;
    const tableMiddle = tableTop + TABLE_HEIGHT / 2;

    const seats: Seat[] = [
        ...rowPositions(topCount, tableWidth).map((x) => ({
            left: tableLeft + x,
            top: tableTop + SEAT_OVERLAP,
            transform: 'translate(-50%, -100%)',
        })),
        ...Array.from({ length: rightCount }, () => ({
            left: tableLeft + tableWidth - SEAT_OVERLAP,
            top: tableMiddle,
            transform: 'translate(0, -50%)',
        })),
        ...rowPositions(bottomCount, tableWidth).reverse().map((x) => ({
            left: tableLeft + x,
            top: tableBottom - SEAT_OVERLAP,
            transform: 'translate(-50%, 0)',
        })),
        ...Array.from({ length: leftCount }, () => ({
            left: tableLeft + SEAT_OVERLAP,
            top: tableMiddle,
            transform: 'translate(-100%, -50%)',
        })),
    ];

    return {
        width: tableWidth + 2 * tableLeft,
        height: tableBottom + (bottomCount > 0 ? rowHeight : 0),
        tableLeft,
        tableWidth,
        tableTop,
        seats,
    };
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
        <div className="flex items-center gap-2">
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
            <div className="min-w-0 text-left">
                <p className="text-[11px] leading-tight text-slate-500 max-w-20 truncate" title={user.name}>
                    {user.name}
                </p>
                {user.subtitle && (
                    <p className="text-[10px] leading-tight text-slate-400 max-w-20 truncate" title={user.subtitle}>
                        {user.subtitle}
                    </p>
                )}
            </div>
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

    const layout = tableLayout(participants.length, availableWidth);
    const useGrid = availableWidth > 0 && availableWidth < GRID_FALLBACK_WIDTH;

    const currentUserSeat = (
        <div className="flex items-center gap-2">
            <div
                ref={currentUserRef}
                className="h-12 w-12 flex-shrink-0 rounded-full overflow-hidden border border-indigo-700 ring-4 ring-indigo-200"
                title={currentUser.name}
                aria-label={currentUser.name}
            >
                <UserAvatar
                    userId={currentUser.id}
                    avatarUrl={currentUser.avatarUrl ?? null}
                    name={currentUser.name}
                    className="h-12 w-12 text-sm border-0"
                    fallbackClassName="bg-indigo-600 text-white"
                />
            </div>
            <div className="min-w-0 text-left">
                <p className="text-[11px] leading-tight text-slate-500 max-w-20 truncate" title={currentUser.name}>
                    {currentUser.name}
                </p>
                {centerAction}
            </div>
        </div>
    );

    return (
        <div ref={containerRef} className="w-full">
            {useGrid ? (
                <div className="flex flex-col items-center gap-4">
                    {currentUserSeat}
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
                <div className="relative mx-auto" style={{ width: layout.width, height: layout.height }}>
                    <div
                        className="absolute rounded-2xl border border-slate-200 bg-slate-100/70 shadow-inner"
                        style={{
                            left: layout.tableLeft,
                            width: layout.tableWidth,
                            top: layout.tableTop,
                            height: TABLE_HEIGHT,
                        }}
                    />

                    <div
                        className="absolute flex flex-col items-center gap-1 text-center -translate-x-1/2 -translate-y-1/2"
                        style={{
                            left: layout.tableLeft + layout.tableWidth / 2,
                            top: layout.tableTop + TABLE_HEIGHT / 2,
                        }}
                    >
                        {currentUserSeat}
                        {participants.length === 0 && (
                            <p className="text-[11px] text-slate-400">No other people yet</p>
                        )}
                    </div>

                    {participants.map((participant, index) => (
                        <div
                            key={participant.id}
                            className="absolute"
                            style={{
                                left: layout.seats[index].left,
                                top: layout.seats[index].top,
                                transform: layout.seats[index].transform,
                            }}
                        >
                            <AvatarPill
                                user={participant}
                                isTargeted={targetedIds.includes(participant.id)}
                                onClick={onParticipantClick && (() => onParticipantClick(participant.id))}
                                avatarRef={participantRef && ((element: HTMLDivElement | null) => participantRef(participant.id, element))}
                            />
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
