import { useState, useMemo, useEffect } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { itemsApi } from '../api/items';
import { ItemCard } from './ItemCard';
import { MergedGroupCard } from './MergedGroupCard';
import type { GetColumnDto, GetItemDto } from '../types';

interface Props {
    column: GetColumnDto;
    retroId: string;
    isClosed: boolean;
    canAddItems: boolean;
    canManage: boolean;
    currentUserId: string;
    isAdmin: boolean;
    isManager: boolean;
    overItemId: string | null;
    onDelete: () => void;
    onRename: (title: string) => void;
    onColorChange: (color: string) => void;
    onMerge: (sourceId: string, targetId: string) => void;
}

export function ColumnView({
    column,
    retroId,
    isClosed,
    canAddItems,
    canManage,
    currentUserId,
    isAdmin,
    isManager,
    overItemId,
    onDelete,
    onRename,
    onColorChange,
    onMerge,
}: Props) {
    const queryClient = useQueryClient();
    const [addingItem, setAddingItem] = useState(false);
    const [newItemText, setNewItemText] = useState('');
    const [editingTitle, setEditingTitle] = useState(false);
    const [titleValue, setTitleValue] = useState(column.title);
    const [mergingFromId, setMergingFromId] = useState<string | null>(null);
    const [localHeaderColor, setLocalHeaderColor] = useState(column.headerColor ?? '#4f46e5');

    useEffect(() => {
        setLocalHeaderColor(column.headerColor ?? '#4f46e5');
    }, [column.headerColor]);

    // Column-level sortable (for column reordering)
    const {
        attributes: colAttributes,
        listeners: colListeners,
        setNodeRef: setColRef,
        transform,
        transition,
        isDragging: isColDragging,
    } = useSortable({
        id: column.id,
        data: { type: 'column', columnId: column.id },
        disabled: !canManage,
    });

    const colStyle = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isColDragging ? 0.4 : 1,
    };

    // Items droppable area
    const { setNodeRef: setDropRef, isOver } = useDroppable({
        id: `col-${column.id}`,
        data: { type: 'column', columnId: column.id },
    });

    const addItemMutation = useMutation({
        mutationFn: (description: string) =>
            itemsApi.create({
                columnId: column.id,
                description,
                position: column.items.length,
            }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospective', retroId] });
            setNewItemText('');
            // Keep the form open so the user can add another item immediately.
            // Press Escape or Cancel to close.
        },
    });

    const unlinkMutation = useMutation({
        mutationFn: (itemId: string) => itemsApi.unlinkFromGroup(itemId),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['retrospective', retroId] }),
    });

    function submitNewItem() {
        const trimmed = newItemText.trim();
        if (trimmed) addItemMutation.mutate(trimmed);
    }

    function saveTitleEdit() {
        const trimmed = titleValue.trim();
        if (trimmed && trimmed !== column.title) onRename(trimmed);
        setEditingTitle(false);
    }

    const sortedItems = [...column.items].sort((a, b) => a.position - b.position);

    type DisplayEntry =
        | { kind: 'single'; item: GetItemDto; sortId: string }
        | { kind: 'group'; items: GetItemDto[]; sortId: string };

    const displayEntries = useMemo((): DisplayEntry[] => {
        const groups = new Map<string, GetItemDto[]>();
        const singles: GetItemDto[] = [];
        for (const item of sortedItems) {
            if (item.groupId) {
                const arr = groups.get(item.groupId) ?? [];
                arr.push(item);
                groups.set(item.groupId, arr);
            } else {
                singles.push(item);
            }
        }
        const entries: DisplayEntry[] = singles.map(item => ({ kind: 'single', item, sortId: item.id }));
        groups.forEach((grpItems) => {
            const grpSorted = [...grpItems].sort((a, b) => a.position - b.position);
            entries.push({ kind: 'group', items: grpSorted, sortId: grpSorted[0].id });
        });
        entries.sort((a, b) => {
            const posA = a.kind === 'single' ? a.item.position : a.items[0].position;
            const posB = b.kind === 'single' ? b.item.position : b.items[0].position;
            return posA - posB;
        });
        return entries;
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [column.items]);

    return (
        <div ref={setColRef} style={colStyle} className="flex flex-col w-72 flex-shrink-0">
            {/* Column header */}
            <div
                className="text-white rounded-t-lg px-2 py-2 flex items-center gap-1"
                style={{ backgroundColor: localHeaderColor }}
            >
                {canManage && (
                    <div
                        {...colAttributes}
                        {...colListeners}
                        className="cursor-grab active:cursor-grabbing text-indigo-300 hover:text-white select-none px-1 flex-shrink-0"
                        title="Drag to reorder"
                    >
                        ⠿
                    </div>
                )}
                <div className="flex-1 min-w-0">
                    {editingTitle ? (
                        <input
                            autoFocus
                            value={titleValue}
                            onChange={(e) => setTitleValue(e.target.value)}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') saveTitleEdit();
                                if (e.key === 'Escape') { setEditingTitle(false); setTitleValue(column.title); }
                            }}
                            onBlur={saveTitleEdit}
                            className="w-full bg-indigo-700 text-white text-sm font-semibold rounded px-1 py-0.5 focus:outline-none focus:ring-1 focus:ring-white"
                        />
                    ) : (
                        <div className="flex items-center gap-1 group/title">
                            <h3 className="font-semibold text-sm truncate">{column.title}</h3>
                            {canManage && (
                                <button
                                    onClick={() => { setTitleValue(column.title); setEditingTitle(true); }}
                                    className="opacity-0 group-hover/title:opacity-100 text-indigo-300 hover:text-white text-xs flex-shrink-0 transition-opacity leading-none"
                                    title="Rename column"
                                >
                                    ✏️
                                </button>
                            )}
                        </div>
                    )}
                    <p className="text-xs text-indigo-200">{column.items.length} item(s)</p>
                </div>
                {canManage && (
                    <button
                        onClick={() => { if (confirm(`Delete "${column.title}"? All items inside will be removed.`)) onDelete(); }}
                        className="text-white/60 hover:text-red-300 flex-shrink-0 px-1 text-sm transition-colors"
                        title="Delete column"
                    >
                        ✕
                    </button>
                )}
                {canManage && (
                    <label className="flex-shrink-0 cursor-pointer" title="Change header colour">
                        <span className="text-xs">🎨</span>
                        <input
                            type="color"
                            className="sr-only"
                            value={localHeaderColor}
                            onChange={(e) => setLocalHeaderColor(e.target.value)}
                            onBlur={(e) => {
                                if (e.target.value !== (column.headerColor ?? '#4f46e5')) {
                                    onColorChange(e.target.value);
                                }
                            }}
                        />
                    </label>
                )}
            </div>

            {/* Items area */}
            <div
                ref={setDropRef}
                className={[
                    'flex-1 bg-slate-100 rounded-b-lg p-2 space-y-2 min-h-[120px] transition-colors',
                    isOver ? 'bg-indigo-50 ring-2 ring-indigo-300' : '',
                ].join(' ')}
            >
                {mergingFromId && (
                    <div className="flex items-center justify-between bg-indigo-50 border border-indigo-200 rounded-md px-2 py-1.5 text-xs">
                        <span className="text-indigo-700 font-medium">Click an item to merge into it</span>
                        <button
                            onClick={() => setMergingFromId(null)}
                            className="text-indigo-400 hover:text-indigo-700 ml-2"
                        >
                            Cancel
                        </button>
                    </div>
                )}
                <SortableContext
                    items={displayEntries.map((e) => e.sortId)}
                    strategy={verticalListSortingStrategy}
                >
                    {displayEntries.map((entry) => {
                        if (entry.kind === 'group') {
                            const repId = entry.sortId;
                            const canMerge = !isClosed && displayEntries.length > 1;
                            return (
                                <MergedGroupCard
                                    key={repId}
                                    representativeId={repId}
                                    items={entry.items}
                                    isClosed={isClosed}
                                    canLink={canAddItems}
                                    isMergeSource={mergingFromId === repId}
                                    isMergeTarget={
                                        overItemId === repId ||
                                        (mergingFromId !== null && mergingFromId !== repId)
                                    }
                                    onMergeStart={canMerge && mergingFromId === null ? () => setMergingFromId(repId) : undefined}
                                    onMergeInto={mergingFromId !== null && mergingFromId !== repId ? () => {
                                        onMerge(mergingFromId, repId);
                                        setMergingFromId(null);
                                    } : undefined}
                                    onUnlink={canAddItems ? (itemId) => unlinkMutation.mutate(itemId) : undefined}
                                />
                            );
                        }
                        const item = entry.item;
                        const canEditItem = isAdmin || isManager || item.createdBy === currentUserId;
                        const canMergeItem = canAddItems && !isClosed && displayEntries.length > 1;
                        return (
                            <ItemCard
                                key={item.id}
                                item={item}
                                retroId={retroId}
                                isClosed={isClosed}
                                isMergeSource={mergingFromId === item.id}
                                isMergeTarget={
                                    overItemId === item.id ||
                                    (mergingFromId !== null && item.id !== mergingFromId)
                                }
                                onMergeStart={canMergeItem && mergingFromId === null ? () => setMergingFromId(item.id) : undefined}
                                onMergeInto={mergingFromId !== null && item.id !== mergingFromId ? () => {
                                    onMerge(mergingFromId, item.id);
                                    setMergingFromId(null);
                                } : undefined}
                                canEdit={canEditItem}
                                canDelete={canEditItem}
                            />
                        );
                    })}
                </SortableContext>

                {!isClosed && canAddItems && (
                    <>
                        {addingItem ? (
                            <div className="space-y-2">
                                <textarea
                                    autoFocus
                                    value={newItemText}
                                    onChange={(e) => setNewItemText(e.target.value)}
                                    onKeyDown={(e) => {
                                        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); submitNewItem(); }
                                        if (e.key === 'Escape') setAddingItem(false);
                                    }}
                                    rows={3}
                                    placeholder="What's on your mind?"
                                    className="w-full text-sm border border-slate-300 rounded-md px-2 py-1.5 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white"
                                />
                                <div className="flex gap-2">
                                    <button
                                        onClick={submitNewItem}
                                        disabled={addItemMutation.isPending}
                                        className="px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                                    >
                                        Add
                                    </button>
                                    <button
                                        onClick={() => { setAddingItem(false); setNewItemText(''); }}
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
                                + Add item
                            </button>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}
