import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { actionItemsApi } from '../api/actionItems';
import type { GetActionItemDto } from '../types';

interface Props {
    item: GetActionItemDto;
    retroId: string;
    assigneeOptions: string[];
    isClosed: boolean;
    canEdit: boolean;
    canDelete: boolean;
    canComplete: boolean;
}

export function ActionItemCard({ item, retroId, assigneeOptions, isClosed, canEdit, canDelete, canComplete }: Props) {
    const queryClient = useQueryClient();
    const [editing, setEditing] = useState(false);
    const [description, setDescription] = useState(item.description);
    const [assignee, setAssignee] = useState(item.assignee);

    const participantAssignees = useMemo(() => {
        const unique = new Set(assigneeOptions.map((name) => name.trim()).filter(Boolean));
        if (item.assignee.trim()) unique.add(item.assignee.trim());
        return Array.from(unique).sort((a, b) => a.localeCompare(b));
    }, [assigneeOptions, item.assignee]);

    const availableAssignees = useMemo(
        () => ['', 'all', ...participantAssignees],
        [participantAssignees]
    );

    useEffect(() => {
        if (!editing) return;
        if (!availableAssignees.includes(assignee)) {
            setAssignee('');
        }
    }, [editing, assignee, availableAssignees]);

    function invalidate() {
        queryClient.invalidateQueries({ queryKey: ['retrospective', retroId] });
    }

    const updateMutation = useMutation({
        mutationFn: () =>
            actionItemsApi.update(item.id, {
                description: description.trim() || undefined,
                assignee: assignee.trim(),
            }),
        onSuccess: () => { invalidate(); setEditing(false); },
    });

    const closeMutation = useMutation({
        mutationFn: () => actionItemsApi.close(item.id),
        onSuccess: invalidate,
    });

    const deleteMutation = useMutation({
        mutationFn: () => actionItemsApi.delete(item.id),
        onSuccess: invalidate,
    });

    return (
        <div
            className={[
                'bg-white border rounded-lg p-3 shadow-sm group',
                item.isCompleted
                    ? 'border-green-300 bg-green-50'
                    : 'border-slate-200',
            ].join(' ')}
        >
            {editing ? (
                <div className="space-y-2">
                    <textarea
                        autoFocus
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        rows={2}
                        className="w-full text-sm border border-slate-300 rounded px-2 py-1 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                    <select
                        value={assignee}
                        onChange={(e) => setAssignee(e.target.value)}
                        className="w-full text-sm border border-slate-300 rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                        <option value="">--</option>
                        <option value="all">all</option>
                        {participantAssignees.map((name) => (
                            <option key={name} value={name}>{name}</option>
                        ))}
                    </select>
                    <div className="flex gap-2">
                        <button
                            onClick={() => updateMutation.mutate()}
                            disabled={updateMutation.isPending}
                            className="px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                        >
                            Save
                        </button>
                        <button
                            onClick={() => { setEditing(false); setDescription(item.description); setAssignee(item.assignee); }}
                            className="px-3 py-1 text-xs text-slate-600 hover:text-slate-800"
                        >
                            Cancel
                        </button>
                    </div>
                </div>
            ) : (
                <div className="flex items-start gap-2">
                    <div className="flex-1">
                        <p className={`text-sm whitespace-pre-wrap break-words ${item.isCompleted ? 'line-through text-slate-400' : 'text-slate-800'}`}>
                            {item.description}
                        </p>
                        <p className="text-xs text-slate-500 mt-1">
                            Assignee: <span className="font-medium">{item.assignee || '—'}</span>
                            {item.iterations > 1 && (
                                <span className="ml-2 text-amber-600">↻ {item.iterations} sprints</span>
                            )}
                        </p>
                        {item.isCompleted && item.closedAt && (
                            <p className="text-xs text-green-600 mt-0.5">
                                Completed {new Date(item.closedAt).toLocaleDateString()}
                            </p>
                        )}
                    </div>
                    {!isClosed && (
                        <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                            {!item.isCompleted && canComplete && (
                                <button
                                    onClick={() => closeMutation.mutate()}
                                    disabled={closeMutation.isPending}
                                    className="text-xs text-slate-400 hover:text-green-600 px-1"
                                    title="Mark complete"
                                >
                                    ✓
                                </button>
                            )}
                            {canEdit && (
                                <button
                                    onClick={() => setEditing(true)}
                                    className="text-xs text-slate-400 hover:text-indigo-600 px-1"
                                    title="Edit"
                                >
                                    ✏️
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
