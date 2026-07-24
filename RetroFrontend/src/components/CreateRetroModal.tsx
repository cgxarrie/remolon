import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';

const DEFAULT_COLUMNS = ['What went well', 'What could be improved', 'What confused me'];

interface Props {
    onClose: () => void;
}

export function CreateRetroModal({ onClose }: Props) {
    const queryClient = useQueryClient();
    const [title, setTitle] = useState('');
    const [columns, setColumns] = useState<string[]>(DEFAULT_COLUMNS);
    const [newCol, setNewCol] = useState('');

    const mutation = useMutation({
        mutationFn: () =>
            retrospectivesApi.create({
                title,
                columns: columns.map((c, i) => ({ title: c, position: i })),
            }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            onClose();
        },
    });

    function addColumn() {
        const trimmed = newCol.trim();
        if (trimmed) {
            setColumns((prev) => [...prev, trimmed]);
            setNewCol('');
        }
    }

    function removeColumn(i: number) {
        setColumns((prev) => prev.filter((_, idx) => idx !== i));
    }

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-xl w-full max-w-md">
                <div className="flex items-center justify-between px-6 py-4 border-b">
                    <h2 className="text-lg font-semibold">New Retrospective</h2>
                    <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">
                        ✕
                    </button>
                </div>
                <div className="p-6 space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Title</label>
                        <input
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="e.g. Sprint 42 Retrospective"
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Columns</label>
                        <ul className="space-y-2 mb-2">
                            {columns.map((col, i) => (
                                <li key={i} className="flex items-center gap-2">
                                    <span className="flex-1 text-sm bg-slate-50 border border-slate-200 rounded-md px-3 py-1.5">
                                        {col}
                                    </span>
                                    <button
                                        onClick={() => removeColumn(i)}
                                        className="text-red-400 hover:text-red-600 text-sm"
                                    >
                                        ✕
                                    </button>
                                </li>
                            ))}
                        </ul>
                        <div className="flex gap-2">
                            <input
                                type="text"
                                value={newCol}
                                onChange={(e) => setNewCol(e.target.value)}
                                onKeyDown={(e) => e.key === 'Enter' && addColumn()}
                                placeholder="Add column…"
                                className="flex-1 border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                            <button
                                onClick={addColumn}
                                className="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md text-sm"
                            >
                                Add
                            </button>
                        </div>
                    </div>
                    {mutation.isError && (
                        <p className="text-sm text-red-600">Failed to create retrospective.</p>
                    )}
                </div>
                <div className="flex justify-end gap-3 px-6 py-4 border-t">
                    <button
                        onClick={onClose}
                        className="px-4 py-2 text-sm text-slate-600 hover:text-slate-800"
                    >
                        Cancel
                    </button>
                    <button
                        onClick={() => mutation.mutate()}
                        disabled={!title.trim() || mutation.isPending}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium rounded-md transition-colors"
                    >
                        {mutation.isPending ? 'Creating…' : 'Create'}
                    </button>
                </div>
            </div>
        </div>
    );
}
