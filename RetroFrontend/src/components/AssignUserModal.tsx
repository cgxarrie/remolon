import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { assignmentsApi } from '../api/assignments';

interface Props {
    retroId: string;
    onClose: () => void;
}

export function AssignUserModal({ retroId, onClose }: Props) {
    const queryClient = useQueryClient();
    const [search, setSearch] = useState('');
    const [selectedUserIds, setSelectedUserIds] = useState<Set<string>>(new Set());
    const [selectionInitialized, setSelectionInitialized] = useState(false);
    const [message, setMessage] = useState('');

    const usersQuery = useQuery({
        queryKey: ['retrospectiveUsers', retroId],
        queryFn: () => assignmentsApi.getRetrospectiveUsers(retroId),
    });

    const participantsQuery = useQuery({
        queryKey: ['retrospectiveParticipants', retroId],
        queryFn: () => assignmentsApi.getRetrospectiveParticipants(retroId),
    });

    const assignedUserIds = useMemo(
        () => new Set(participantsQuery.data?.map((participant) => participant.id) ?? []),
        [participantsQuery.data],
    );
    useEffect(() => {
        if (participantsQuery.data && !selectionInitialized) {
            setSelectedUserIds(new Set(participantsQuery.data.map((participant) => participant.id)));
            setSelectionInitialized(true);
        }
    }, [participantsQuery.data, selectionInitialized]);
    const hasChanges = useMemo(
        () => selectedUserIds.size !== assignedUserIds.size
            || [...selectedUserIds].some((id) => !assignedUserIds.has(id)),
        [assignedUserIds, selectedUserIds],
    );
    const filteredUsers = useMemo(() => {
        const query = search.trim().toLowerCase();
        if (!query) return usersQuery.data ?? [];
        return (usersQuery.data ?? []).filter(
            (user) =>
                user.nickname.toLowerCase().includes(query)
                || user.email.toLowerCase().includes(query),
        );
    }, [search, usersQuery.data]);

    const saveMutation = useMutation({
        mutationFn: () => assignmentsApi.assignBatch({
            retrospectiveId: retroId,
            userIds: [...selectedUserIds],
        }),
        onSuccess: async ({ assignedCount, removedCount }) => {
            await queryClient.invalidateQueries({ queryKey: ['retrospectiveParticipants', retroId] });
            setMessage(`${assignedCount} assigned, ${removedCount} removed.`);
        },
        onError: (err: unknown) => {
            const axiosError = err as { response?: { data?: { message?: string } } };
            setMessage(axiosError.response?.data?.message ?? 'Failed to save participants.');
        },
    });

    const toggleUser = (userId: string) => {
        setMessage('');
        setSelectedUserIds((current) => {
            const next = new Set(current);
            if (next.has(userId)) next.delete(userId);
            else next.add(userId);
            return next;
        });
    };

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-slate-900 rounded-xl shadow-xl w-full max-w-lg">
                <div className="flex items-center justify-between px-6 py-4 border-b">
                    <h2 className="text-lg font-semibold">Manage Participants</h2>
                    <button
                        onClick={onClose}
                        aria-label="Close"
                        className="text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:hover:text-slate-300 text-xl"
                    >
                        ✕
                    </button>
                </div>
                <div className="p-6 space-y-4">
                    <input
                        type="search"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                        placeholder="Search by nickname or email"
                        aria-label="Search organization users"
                        className="w-full border border-slate-300 dark:border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />

                    <div className="border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden">
                        <div className="px-4 py-2 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-700 text-xs font-medium text-slate-500 dark:text-slate-400">
                            Organization users
                        </div>
                        <div className="max-h-80 overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800">
                            {(usersQuery.isPending || participantsQuery.isPending) && (
                                <p className="px-4 py-8 text-sm text-slate-500 dark:text-slate-400 text-center">Loading users…</p>
                            )}
                            {(usersQuery.isError || participantsQuery.isError) && (
                                <p className="px-4 py-8 text-sm text-red-600 dark:text-red-400 text-center">
                                    Failed to load organization users.
                                </p>
                            )}
                            {!usersQuery.isPending && !participantsQuery.isPending
                                && !usersQuery.isError && !participantsQuery.isError
                                && filteredUsers.length === 0 && (
                                <p className="px-4 py-8 text-sm text-slate-500 dark:text-slate-400 text-center">
                                    {search.trim() ? 'No users match your search.' : 'No users found.'}
                                </p>
                            )}
                            {!participantsQuery.isPending && filteredUsers.map((user) => {
                                const isAssigned = assignedUserIds.has(user.id);
                                const isSelected = selectedUserIds.has(user.id);
                                return (
                                    <label
                                        key={user.id}
                                        className="flex items-center gap-3 px-4 py-3 hover:bg-indigo-50 dark:hover:bg-indigo-950/40 cursor-pointer"
                                    >
                                        <input
                                            type="checkbox"
                                            checked={isSelected}
                                            onChange={() => toggleUser(user.id)}
                                            className="h-4 w-4 rounded border-slate-300 dark:border-slate-600 text-indigo-600 dark:text-indigo-400 focus:ring-indigo-500"
                                        />
                                        <span className="min-w-0 flex-1">
                                            <span className="flex items-center gap-2">
                                                <span className="block text-sm font-medium text-slate-800 dark:text-slate-100 truncate">
                                                    {user.nickname}
                                                </span>
                                                {isAssigned && isSelected && (
                                                    <span className="shrink-0 text-[10px] font-semibold uppercase tracking-wide text-emerald-700 dark:text-emerald-400 bg-emerald-50 rounded-full px-2 py-0.5">
                                                        Assigned
                                                    </span>
                                                )}
                                            </span>
                                            <span className="block text-xs text-slate-500 dark:text-slate-400 truncate">
                                                {user.email} · {user.role === 'StandardUser' ? 'Standard User' : user.role}
                                            </span>
                                        </span>
                                    </label>
                                );
                            })}
                        </div>
                    </div>
                    {message && (
                        <p className={`text-sm rounded-md border p-3 ${
                            saveMutation.isError
                                ? 'text-red-700 bg-red-50 dark:bg-red-950/40 border-red-200 dark:border-red-900'
                                : 'text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-slate-800/60 border-slate-200 dark:border-slate-700'
                        }`}>
                            {message}
                        </p>
                    )}
                </div>
                <div className="flex justify-end gap-3 px-6 py-4 border-t">
                    <button onClick={onClose} className="px-4 py-2 text-sm text-slate-600 dark:text-slate-300 hover:text-slate-800 dark:hover:text-slate-100">
                        Close
                    </button>
                    <button
                        onClick={() => saveMutation.mutate()}
                        disabled={!hasChanges || saveMutation.isPending}
                        className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
                    >
                        {saveMutation.isPending ? 'Saving…' : 'Save participants'}
                    </button>
                </div>
            </div>
        </div>
    );
}
