import { useState } from 'react';
import {
    DndContext,
    DragEndEvent,
    DragOverEvent,
    DragStartEvent,
    PointerSensor,
    useSensor,
    useSensors,
    closestCenter,
    DragOverlay,
} from '@dnd-kit/core';
import { SortableContext, arrayMove, horizontalListSortingStrategy } from '@dnd-kit/sortable';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { itemsApi } from '../api/items';
import { retrospectivesApi } from '../api/retrospectives';
import { ColumnView } from './ColumnView';
import { ActionColumnView } from './ActionColumnView';
import type { GetColumnDto, GetItemDto, GetRetrospectiveDto } from '../types';
import { useAuthStore } from '../store/authStore';

interface Props {
    retro: GetRetrospectiveDto;
}

export function RetroBoard({ retro }: Props) {
    const queryClient = useQueryClient();
    const { userId, role } = useAuthStore();
    const isAdmin = role === 'Admin';
    const isManager = role === 'Manager';
    const canManage = (isAdmin || isManager) && !retro.isClosed;
    const currentUserId = userId ?? '';

    const [activeItem, setActiveItem] = useState<GetItemDto | null>(null);
    const [activeColumn, setActiveColumn] = useState<GetColumnDto | null>(null);
    const [overItemId, setOverItemId] = useState<string | null>(null);
    const [addingColumn, setAddingColumn] = useState(false);
    const [newColumnTitle, setNewColumnTitle] = useState('');

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 8 } })
    );

    function invalidate() {
        queryClient.invalidateQueries({ queryKey: ['retrospective', retro.id] });
    }

    function findItem(id: string): GetItemDto | undefined {
        for (const col of retro.columns) {
            const found = col.items.find((i) => i.id === id);
            if (found) return found;
        }
        return undefined;
    }

    function findColumnId(itemId: string): string | undefined {
        return retro.columns.find((c) => c.items.some((i) => i.id === itemId))?.id;
    }

    const reorderItemMutation = useMutation({
        mutationFn: ({ id, position }: { id: string; position: number }) =>
            itemsApi.update(id, { position }),
        onSuccess: invalidate,
    });

    const mergeItemMutation = useMutation({
        mutationFn: ({ sourceId, targetId }: { sourceId: string; targetId: string }) =>
            itemsApi.merge(sourceId, targetId),
        onSuccess: invalidate,
    });

    const addColumnMutation = useMutation({
        mutationFn: (title: string) =>
            retrospectivesApi.update(retro.id, {
                addColumns: [{ title, position: retro.columns.length }],
            }),
        onSuccess: () => { invalidate(); setNewColumnTitle(''); setAddingColumn(false); },
    });

    const deleteColumnMutation = useMutation({
        mutationFn: (columnId: string) =>
            retrospectivesApi.update(retro.id, { removeColumnIds: [columnId] }),
        onSuccess: invalidate,
    });

    const renameColumnMutation = useMutation({
        mutationFn: ({ id, title }: { id: string; title: string }) =>
            retrospectivesApi.update(retro.id, { updateColumns: [{ id, title }] }),
        onSuccess: invalidate,
    });

    const changeColorMutation = useMutation({
        mutationFn: ({ id, headerColor }: { id: string; headerColor: string }) =>
            retrospectivesApi.update(retro.id, { updateColumns: [{ id, headerColor }] }),
        onSuccess: invalidate,
    });

    const reorderColumnsMutation = useMutation({
        mutationFn: (updateColumns: { id: string; position: number }[]) =>
            retrospectivesApi.update(retro.id, { updateColumns }),
        onSuccess: invalidate,
    });

    function handleDragStart(event: DragStartEvent) {
        const type = event.active.data.current?.type;
        if (type === 'column') {
            const col = retro.columns.find((c) => c.id === String(event.active.id));
            if (col) setActiveColumn(col);
        } else {
            const item = findItem(String(event.active.id));
            if (item) setActiveItem(item);
        }
    }

    function handleDragOver(event: DragOverEvent) {
        const { active, over } = event;
        if (!over || active.data.current?.type === 'column') { setOverItemId(null); return; }
        const overType = over.data.current?.type;
        if (overType === 'item') {
            const activeColId = findColumnId(String(active.id));
            const overColId = findColumnId(String(over.id));
            if (activeColId && overColId && activeColId !== overColId) {
                setOverItemId(String(over.id));
            } else {
                setOverItemId(null);
            }
        } else {
            setOverItemId(null);
        }
    }

    function handleDragEnd(event: DragEndEvent) {
        setActiveItem(null);
        setActiveColumn(null);
        setOverItemId(null);

        const { active, over } = event;
        if (!over || active.id === over.id) return;

        const activeId = String(active.id);
        const overId = String(over.id);
        const activeType = active.data.current?.type;
        const overType = over.data.current?.type;

        // Column reorder
        if (activeType === 'column') {
            if (overType !== 'column') return;
            const sorted = [...retro.columns].sort((a, b) => a.position - b.position);
            const oldIndex = sorted.findIndex((c) => c.id === activeId);
            const newIndex = sorted.findIndex((c) => c.id === overId);
            if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;
            const reordered = arrayMove(sorted, oldIndex, newIndex);
            reorderColumnsMutation.mutate(
                reordered.map((col, idx) => ({ id: col.id, position: idx }))
            );
            return;
        }

        // Item drag
        const activeColId = findColumnId(activeId);
        if (overType === 'item') {
            const overColId = findColumnId(overId);
            if (activeColId && overColId && activeColId === overColId) {
                // Same column → reorder
                const column = retro.columns.find((c) => c.id === activeColId);
                if (!column) return;
                const sorted = [...column.items].sort((a, b) => a.position - b.position);
                const oldIndex = sorted.findIndex((i) => i.id === activeId);
                const newIndex = sorted.findIndex((i) => i.id === overId);
                if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;
                const reordered = arrayMove(sorted, oldIndex, newIndex);
                reordered.forEach((item, idx) => {
                    if (item.position !== idx) {
                        reorderItemMutation.mutate({ id: item.id, position: idx });
                    }
                });
            } else {
                // Cross-column → merge
                mergeItemMutation.mutate({ sourceId: activeId, targetId: overId });
            }
        }
    }

    function submitAddColumn() {
        const trimmed = newColumnTitle.trim();
        if (trimmed) addColumnMutation.mutate(trimmed);
    }

    const sortedColumns = [...retro.columns].sort((a, b) => a.position - b.position);

    return (
        <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragStart={handleDragStart}
            onDragOver={handleDragOver}
            onDragEnd={handleDragEnd}
        >
            <div className="space-y-8">
                {/* Regular columns */}
                <section>
                    <h2 className="text-base font-semibold text-slate-600 mb-3">Board</h2>
                    <div className="flex gap-4 overflow-x-auto pb-4 items-start">
                        <SortableContext
                            items={sortedColumns.map((c) => c.id)}
                            strategy={horizontalListSortingStrategy}
                        >
                            {sortedColumns.map((col) => (
                                <ColumnView
                                    key={col.id}
                                    column={col}
                                    retroId={retro.id}
                                    isClosed={retro.isClosed}
                                    canAddItems={true}
                                    canManage={canManage}
                                    currentUserId={currentUserId}
                                    isAdmin={isAdmin}
                                    isManager={isManager}
                                    overItemId={overItemId}
                                    onDelete={() => deleteColumnMutation.mutate(col.id)}
                                    onRename={(title) => renameColumnMutation.mutate({ id: col.id, title })}
                                    onColorChange={(headerColor) => changeColorMutation.mutate({ id: col.id, headerColor })}
                                    onMerge={(sourceId, targetId) => mergeItemMutation.mutate({ sourceId, targetId })}
                                />
                            ))}
                        </SortableContext>

                        {canManage && (
                            <div className="w-64 flex-shrink-0">
                                {addingColumn ? (
                                    <div className="bg-white border-2 border-dashed border-indigo-300 rounded-lg p-3 space-y-2">
                                        <input
                                            autoFocus
                                            value={newColumnTitle}
                                            onChange={(e) => setNewColumnTitle(e.target.value)}
                                            onKeyDown={(e) => {
                                                if (e.key === 'Enter') submitAddColumn();
                                                if (e.key === 'Escape') { setAddingColumn(false); setNewColumnTitle(''); }
                                            }}
                                            placeholder="Column title…"
                                            className="w-full text-sm border border-slate-300 rounded-md px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                                        />
                                        <div className="flex gap-2">
                                            <button
                                                onClick={submitAddColumn}
                                                disabled={addColumnMutation.isPending || !newColumnTitle.trim()}
                                                className="px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                                            >
                                                Add
                                            </button>
                                            <button
                                                onClick={() => { setAddingColumn(false); setNewColumnTitle(''); }}
                                                className="px-3 py-1 text-xs text-slate-600 hover:text-slate-800"
                                            >
                                                Cancel
                                            </button>
                                        </div>
                                    </div>
                                ) : (
                                    <button
                                        onClick={() => setAddingColumn(true)}
                                        className="w-full h-12 text-sm text-slate-400 hover:text-indigo-600 border-2 border-dashed border-slate-300 hover:border-indigo-400 rounded-lg transition-colors"
                                    >
                                        + Add Column
                                    </button>
                                )}
                            </div>
                        )}
                    </div>
                </section>

                {/* Action item columns */}
                {retro.actionColumns.length > 0 && (
                    <section>
                        <h2 className="text-base font-semibold text-slate-600 mb-3">Action Items</h2>
                        <div className="flex gap-4 overflow-x-auto pb-4">
                            {[...retro.actionColumns]
                                .sort((a, b) => a.position - b.position)
                                .map((col) => (
                                    <ActionColumnView
                                        key={col.id}
                                        column={col}
                                        retroId={retro.id}
                                        isClosed={retro.isClosed}
                                        canAddItems={true}
                                        currentUserId={currentUserId}
                                        isAdmin={isAdmin}
                                        isManager={isManager}
                                    />
                                ))}
                        </div>
                    </section>
                )}
            </div>

            <DragOverlay>
                {activeItem && (
                    <div className="bg-white border border-indigo-400 rounded-lg p-3 shadow-xl w-72 opacity-90">
                        <p className="text-sm text-slate-800">{activeItem.description}</p>
                    </div>
                )}
                {activeColumn && (
                    <div className="bg-indigo-600 text-white rounded-lg px-3 py-2 shadow-xl w-72 opacity-90">
                        <p className="font-semibold text-sm">{activeColumn.title}</p>
                        <p className="text-xs text-indigo-200">{activeColumn.items.length} item(s)</p>
                    </div>
                )}
            </DragOverlay>
        </DndContext>
    );
}
