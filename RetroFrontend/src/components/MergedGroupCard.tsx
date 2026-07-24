import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { GetItemDto } from '../types';

interface Props {
    representativeId: string;
    items: GetItemDto[];
    isClosed: boolean;
    canLink: boolean;
    isMergeSource?: boolean;
    isMergeTarget?: boolean;
    onMergeStart?: () => void;
    onMergeInto?: () => void;
    onUnlink?: (itemId: string) => void;
}

export function MergedGroupCard({
    representativeId,
    items,
    isClosed,
    canLink,
    isMergeSource,
    isMergeTarget,
    onMergeStart,
    onMergeInto,
    onUnlink,
}: Props) {
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: representativeId,
        data: { type: 'item', item: items[0] },
        disabled: isClosed,
    });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.4 : 1,
    };

    const borderClass = isMergeSource
        ? 'border border-blue-400 ring-2 ring-blue-300 bg-blue-50 border-l-4 border-l-blue-400'
        : isMergeTarget
            ? 'border border-indigo-400 ring-2 ring-indigo-300 border-l-4 border-l-amber-400'
            : 'border border-slate-200 border-l-4 border-l-amber-400';

    return (
        <div
            ref={setNodeRef}
            style={style}
            onClick={onMergeInto}
            className={[
                'bg-white rounded-lg p-3 shadow-sm group',
                borderClass,
                onMergeInto ? 'cursor-pointer hover:bg-indigo-50' : '',
            ].join(' ')}
        >
            <div className="flex items-start gap-2">
                {!isClosed && (
                    <div
                        {...attributes}
                        {...listeners}
                        className="mt-0.5 cursor-grab active:cursor-grabbing text-slate-300 hover:text-slate-500 flex-shrink-0 select-none"
                        title="Drag to reorder"
                    >
                        ⠿
                    </div>
                )}
                <div className="flex-1 min-w-0">
                    {items.map((item, i) => (
                        <div
                            key={item.id}
                            className={['group/item flex items-start gap-1', i > 0 ? 'border-t border-amber-100 pt-2 mt-2' : ''].join(' ')}
                        >
                            <div className="flex-1 min-w-0">
                                <p className="text-sm text-slate-800 whitespace-pre-wrap break-words">
                                    {item.description}
                                </p>
                                {item.createdByNickname && (
                                    <p className="text-xs text-slate-400 mt-0.5">
                                        {item.createdByNickname}
                                    </p>
                                )}
                            </div>
                            {!isClosed && canLink && onUnlink && (
                                <button
                                    onClick={(e) => { e.stopPropagation(); onUnlink(item.id); }}
                                    className="opacity-0 group-hover/item:opacity-100 transition-opacity text-xs text-slate-300 hover:text-red-500 flex-shrink-0 mt-0.5"
                                    title="Remove from group"
                                >
                                    ✂️
                                </button>
                            )}
                        </div>
                    ))}
                </div>
                {!isClosed && onMergeStart && (
                    <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                        <button
                            onClick={(e) => { e.stopPropagation(); onMergeStart(); }}
                            className="text-xs text-slate-400 hover:text-indigo-600 px-1"
                            title="Merge with another item"
                        >
                            🔗
                        </button>
                    </div>
                )}
            </div>
            <div className="mt-2 text-xs text-amber-500 flex items-center gap-1">
                <span>🔗</span>
                <span>{items.length} merged</span>
            </div>
        </div>
    );
}
