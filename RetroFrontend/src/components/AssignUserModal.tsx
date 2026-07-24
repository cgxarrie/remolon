import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { assignmentsApi } from '../api/assignments';

interface Props {
    retroId: string;
    onClose: () => void;
}

export function AssignUserModal({ retroId, onClose }: Props) {
    const [email, setEmail] = useState('');
    const [message, setMessage] = useState('');

    const assignMutation = useMutation({
        mutationFn: () => assignmentsApi.assign({ userEmail: email, retrospectiveId: retroId }),
        onSuccess: () => {
            setMessage(`User ${email} assigned successfully.`);
            setEmail('');
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string } } };
            setMessage(axiosError.response?.data?.message ?? 'Failed to assign user.');
        },
    });

    const unassignMutation = useMutation({
        mutationFn: () => assignmentsApi.unassign({ userEmail: email, retrospectiveId: retroId }),
        onSuccess: () => {
            setMessage(`User ${email} removed.`);
            setEmail('');
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string } } };
            setMessage(axiosError.response?.data?.message ?? 'Failed to remove user.');
        },
    });

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-xl w-full max-w-sm">
                <div className="flex items-center justify-between px-6 py-4 border-b">
                    <h2 className="text-lg font-semibold">Manage Participants</h2>
                    <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">
                        ✕
                    </button>
                </div>
                <div className="p-6 space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">User Email</label>
                        <input
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="user@example.com"
                            className="w-full border border-slate-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        />
                    </div>
                    {message && (
                        <p className="text-sm text-slate-600 bg-slate-50 border border-slate-200 rounded-md p-3">
                            {message}
                        </p>
                    )}
                </div>
                <div className="flex justify-end gap-3 px-6 py-4 border-t">
                    <button onClick={onClose} className="px-4 py-2 text-sm text-slate-600 hover:text-slate-800">
                        Close
                    </button>
                    <button
                        onClick={() => unassignMutation.mutate()}
                        disabled={!email.trim() || unassignMutation.isPending}
                        className="px-4 py-2 bg-red-50 hover:bg-red-100 text-red-700 text-sm font-medium rounded-md border border-red-200 transition-colors disabled:opacity-50"
                    >
                        Remove
                    </button>
                    <button
                        onClick={() => assignMutation.mutate()}
                        disabled={!email.trim() || assignMutation.isPending}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium rounded-md transition-colors"
                    >
                        Assign
                    </button>
                </div>
            </div>
        </div>
    );
}
