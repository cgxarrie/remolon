import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { retrospectivesApi } from '../api/retrospectives';
import type { GetRetrospectiveDto } from '../types';
import { invalidateRetrospective } from '../query/retrospectiveQueries';

interface Props {
    retro: GetRetrospectiveDto;
    onClose: () => void;
}

export function EditRetroModal({ retro, onClose }: Props) {
    const queryClient = useQueryClient();
    const [title, setTitle] = useState(retro.title);
    const [newColTitle, setNewColTitle] = useState('');

    const mutation = useMutation({
        mutationFn: () =>
            retrospectivesApi.update(retro.id, {
                title: title !== retro.title ? title : undefined,
                addColumns: newColTitle.trim()
                    ? [{ title: newColTitle.trim(), position: retro.columns.length }]
                    : undefined,
            }),
        onSuccess: () => {
            invalidateRetrospective(queryClient, retro.id);
            queryClient.invalidateQueries({ queryKey: ['retrospectives'] });
            onClose();
        },
    });

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-xl w-full max-w-md">
                <div className="flex items-center justify-between px-6 py-4 border-b">
                    <h2 className="text-lg font-semibold">Edit Retrospective</h2>
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
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">Add Column</label>
                        <input
                            type="text"
                            value={newColTitle}
                            onChange={(e) => setNewColTitle(e.target.value)}
                            placeholder="New column title…"
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                        <p className="text-xs text-slate-400 mt-1">
                            Leave blank to only update the title.
                        </p>
                    </div>
                    {mutation.isError && (
                        <p className="text-sm text-red-600">Failed to save changes.</p>
                    )}
                </div>
                <div className="flex justify-end gap-3 px-6 py-4 border-t">
                    <button onClick={onClose} className="px-4 py-2 text-sm text-slate-600 hover:text-slate-800">
                        Cancel
                    </button>
                    <button
                        onClick={() => mutation.mutate()}
                        disabled={!title.trim() || mutation.isPending}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium rounded-md transition-colors"
                    >
                        {mutation.isPending ? 'Saving…' : 'Save'}
                    </button>
                </div>
            </div>
        </div>
    );
}
