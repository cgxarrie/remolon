import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { actionItemsApi } from '../api/actionItems';
import { ActionItemCard } from './ActionItemCard';
import type { GetActionColumnDto } from '../types';

interface Props {
    column: GetActionColumnDto;
    retroId: string;
    isClosed: boolean;
    canAddItems: boolean;
    currentUserId: string;
    isAdmin: boolean;
    isManager: boolean;
}

export function ActionColumnView({
    column,
    retroId,
    isClosed,
    canAddItems,
    currentUserId,
    isAdmin,
    isManager,
}: Props) {
    const queryClient = useQueryClient();
    const [addingItem, setAddingItem] = useState(false);
    const [description, setDescription] = useState('');
    const [assignee, setAssignee] = useState('');

    const addItemMutation = useMutation({
        mutationFn: () =>
            actionItemsApi.create({
                columnId: column.id,
                description: description.trim(),
                position: column.items.length,
                assignee: assignee.trim(),
            }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospective', retroId] });
            setDescription('');
            setAssignee('');
            setAddingItem(false);
        },
    });

    const sortedItems = [...column.items].sort((a, b) => a.position - b.position);
    const isActionItemsColumn = column.title === 'Action Items';
    const headerColor = column.title.toLowerCase().includes('pending')
        ? 'bg-amber-600'
        : 'bg-emerald-600';

    return (
        <div className="flex flex-col w-72 flex-shrink-0">
            <div className={`${headerColor} text-white rounded-t-lg px-3 py-2`}>
                <h3 className="font-semibold text-sm truncate">{column.title}</h3>
                <p className="text-xs opacity-80">{column.items.length} item(s)</p>
            </div>
            <div className="flex-1 bg-slate-100 rounded-b-lg p-2 space-y-2 min-h-[120px]">
                {sortedItems.map((item) => (
                    <ActionItemCard
                        key={item.id}
                        item={item}
                        retroId={retroId}
                        isClosed={isClosed}
                        canEdit={isAdmin || isManager || item.createdBy === currentUserId}
                        canDelete={isAdmin || isManager || item.createdBy === currentUserId}
                        canComplete={!isActionItemsColumn}
                    />
                ))}

                {!isClosed && canAddItems && (
                    <>
                        {addingItem ? (
                            <div className="space-y-2">
                                <textarea
                                    autoFocus
                                    value={description}
                                    onChange={(e) => setDescription(e.target.value)}
                                    rows={2}
                                    placeholder="Action item description…"
                                    className="w-full text-sm border border-slate-300 rounded-md px-2 py-1.5 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white"
                                />
                                <input
                                    type="text"
                                    value={assignee}
                                    onChange={(e) => setAssignee(e.target.value)}
                                    placeholder="Assignee name or email"
                                    className="w-full text-sm border border-slate-300 rounded-md px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white"
                                />
                                <div className="flex gap-2">
                                    <button
                                        onClick={() => addItemMutation.mutate()}
                                        disabled={addItemMutation.isPending || !description.trim()}
                                        className="px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                                    >
                                        Add
                                    </button>
                                    <button
                                        onClick={() => { setAddingItem(false); setDescription(''); setAssignee(''); }}
                                        className="px-3 py-1 text-xs text-slate-600 hover:text-slate-800"
                                    >
                                        Cancel
                                    </button>
                                </div>
                            </div>
                        ) : (
                            <button
                                onClick={() => setAddingItem(true)}
                                className="w-full text-left text-xs text-slate-400 hover:text-indigo-600 hover:bg-white rounded-md px-2 py-1.5 border border-dashed border-slate-300 hover:border-indigo-300 transition-colors"
                            >
                                + Add action item
                            </button>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}
