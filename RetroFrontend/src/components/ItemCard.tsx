import { useState } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { itemsApi } from '../api/items';
import { ITEM_DESCRIPTION_MAX_LENGTH, type GetItemDto } from '../types';
import { invalidateRetrospective } from '../query/retrospectiveQueries';

interface Props {
    item: GetItemDto;
    retroId: string;
    isClosed: boolean;
    canEdit: boolean;
    canDelete: boolean;
    isMergeTarget?: boolean;
    isMergeSource?: boolean;
    onMergeStart?: () => void;
    onMergeInto?: () => void;
}

export function ItemCard({ item, retroId, isClosed, canEdit, canDelete, isMergeTarget, isMergeSource, onMergeStart, onMergeInto }: Props) {
    const queryClient = useQueryClient();
    const [editing, setEditing] = useState(false);
    const [description, setDescription] = useState(item.description);

    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: item.id,
        data: { type: 'item', item },
        disabled: isClosed,
    });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.4 : 1,
    };

    const updateMutation = useMutation({
        mutationFn: (desc: string) => itemsApi.update(item.id, { description: desc }),
        onSuccess: () => {
            invalidateRetrospective(queryClient, retroId);
            setEditing(false);
        },
    });

    const deleteMutation = useMutation({
        mutationFn: () => itemsApi.delete(item.id),
        onSuccess: () => invalidateRetrospective(queryClient, retroId),
    });

    function saveEdit() {
        const trimmed = description.trim();
        if (trimmed && trimmed !== item.description) {
            updateMutation.mutate(trimmed);
        } else {
            setEditing(false);
        }
    }

    return (
        <div
            ref={setNodeRef}
            style={style}
            onClick={onMergeInto}
            className={[
                'bg-white border rounded-lg p-3 shadow-sm group',
                isMergeSource ? 'border-blue-400 ring-2 ring-blue-300 bg-blue-50' : '',
                !isMergeSource && isMergeTarget ? 'border-indigo-400 ring-2 ring-indigo-300' : '',
                !isMergeSource && !isMergeTarget ? 'border-slate-200' : '',
                onMergeInto ? 'cursor-pointer hover:bg-indigo-50' : '',
            ].join(' ')}
        >
            {editing ? (
                <div className="space-y-2">
                    <textarea
                        autoFocus
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        rows={3}
                        maxLength={ITEM_DESCRIPTION_MAX_LENGTH}
                        className="w-full text-sm border border-slate-300 rounded px-2 py-1 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    <div className="flex gap-2">
                        <button
                            onClick={saveEdit}
                            disabled={updateMutation.isPending}
                            className="px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                        >
                            Save
                        </button>
                        <button
                            onClick={() => { setEditing(false); setDescription(item.description); }}
                            className="px-3 py-1 text-xs text-slate-600 hover:text-slate-800"
                        >
                            Cancel
                        </button>
                    </div>
                </div>
            ) : (
                <div className="flex items-start gap-2">
                    {!isClosed && (
                        <div
                            {...attributes}
                            {...listeners}
                            className="mt-0.5 cursor-grab active:cursor-grabbing text-slate-300 hover:text-slate-500 flex-shrink-0 select-none"
                            title="Drag to reorder or merge"
                        >
                            ⠿
                        </div>
                    )}
                    <div className="flex-1 min-w-0">
                        <p className="text-sm text-slate-800 whitespace-pre-wrap break-words">
                            {item.description}
                        </p>
                        {item.createdByNickname && (
                            <p className="text-xs text-slate-400 mt-1">
                                {item.createdByNickname}
                            </p>
                        )}
                    </div>
                    {!isClosed && (
                        <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                            {canEdit && (
                                <button
                                    onClick={() => setEditing(true)}
                                    className="text-xs text-slate-400 hover:text-indigo-600 px-1"
                                    title="Edit"
                                >
                                    ✏️
                                </button>
                            )}
                            {onMergeStart && (
                                <button
                                    onClick={(e) => { e.stopPropagation(); onMergeStart(); }}
                                    className="text-xs text-slate-400 hover:text-indigo-600 px-1"
                                    title="Merge with another item"
                                >
                                    🔗
                                </button>
                            )}
                            {canDelete && (
                                <button
                                    onClick={() => deleteMutation.mutate()}
                                    disabled={deleteMutation.isPending}
                                    className="text-xs text-slate-400 hover:text-red-600 px-1"
                                    title="Delete"
                                >
                                    🗑️
                                </button>
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
