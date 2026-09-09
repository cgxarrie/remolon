import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { actionItemsApi } from '../api/actionItems';
import { ActionItemCard } from './ActionItemCard';
import { AssigneePicker } from './AssigneePicker';
import { ITEM_DESCRIPTION_MAX_LENGTH, type GetActionColumnDto } from '../types';
import { invalidateRetrospective } from '../query/retrospectiveQueries';

interface Props {
    column: GetActionColumnDto;
    retroId: string;
    isClosed: boolean;
    canAddItems: boolean;
    assigneeOptions: string[];
    currentUserId: string;
    isManager: boolean;
}

export function ActionColumnView({
    column,
    retroId,
    isClosed,
    canAddItems,
    assigneeOptions,
    currentUserId,
    isManager,
}: Props) {
    const queryClient = useQueryClient();
    const [addingItem, setAddingItem] = useState(false);
    const [description, setDescription] = useState('');
    const [assignees, setAssignees] = useState<string[]>([]);
    const [error, setError] = useState('');

    const participantAssignees = useMemo(() => {
        const unique = new Set(assigneeOptions.map((name) => name.trim()).filter(Boolean));
        return Array.from(unique).sort((a, b) => a.localeCompare(b));
    }, [assigneeOptions]);

    useEffect(() => {
        setAssignees((current) => current.filter((name) => name === 'all' || participantAssignees.includes(name)));
    }, [participantAssignees]);

    const addItemMutation = useMutation({
        mutationFn: () =>
            actionItemsApi.create({
                columnId: column.id,
                description: description.trim(),
                position: column.items.length,
                assignees,
            }),
        onSuccess: () => {
            invalidateRetrospective(queryClient, retroId);
            setDescription('');
            setAssignees([]);
            setError('');
            setAddingItem(false);
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { status?: number; data?: { message?: string } } };
            if (axiosError.response?.status === 403) {
                setError('You are not allowed to add action items to this retrospective.');
                return;
            }

            setError(axiosError.response?.data?.message ?? 'Could not save the action item.');
        },
    });

    const sortedItems = [...column.items].sort((a, b) => a.position - b.position);
    const isActionItemsColumn = column.title === 'Action Items';
    const isPendingActionItemsColumn = column.title.toLowerCase().includes('pending');
    const headerColor = column.title.toLowerCase().includes('pending')
        ? 'bg-amber-600'
        : 'bg-emerald-600';

    return (
        <div className="flex flex-col w-full">
            <div className={`${headerColor} text-white rounded-t-lg px-3 py-2`}>
                <h3 className="font-semibold text-sm truncate">{column.title}</h3>
                <p className="text-xs opacity-80">{column.items.length} item(s)</p>
            </div>
            <div className="bg-slate-100 rounded-b-lg p-2 grid gap-2 min-h-[120px] items-start grid-cols-[repeat(auto-fill,minmax(16rem,1fr))]">
                {sortedItems.map((item) => (
                    <ActionItemCard
                        key={item.id}
                        item={item}
                        retroId={retroId}
                        assigneeOptions={participantAssignees}
                        isClosed={isClosed}
                        canEdit={isManager || item.createdBy === currentUserId}
                        canDelete={isManager || item.createdBy === currentUserId}
                        canComplete={!isActionItemsColumn}
                    />
                ))}

                {!isClosed && canAddItems && !isPendingActionItemsColumn && (
                    <>
                        {addingItem ? (
                            <div className="space-y-2">
                                <textarea
                                    autoFocus
                                    value={description}
                                    onChange={(e) => setDescription(e.target.value)}
                                    rows={2}
                                    maxLength={ITEM_DESCRIPTION_MAX_LENGTH}
                                    placeholder="Action item description…"
                                    className="w-full text-sm border border-slate-300 rounded-md px-2 py-1.5 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white"
                                />
                                <AssigneePicker
                                    options={participantAssignees}
                                    selected={assignees}
                                    onChange={setAssignees}
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
                                        onClick={() => { setAddingItem(false); setDescription(''); setAssignees([]); setError(''); }}
                                        className="px-3 py-1 text-xs text-slate-600 hover:text-slate-800"
                                    >
                                        Cancel
                                    </button>
                                </div>
                                {error && <p className="text-xs text-red-600">{error}</p>}
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
