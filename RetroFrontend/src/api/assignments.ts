import client from './client';
import type { AssignUserRequest } from '../types';

export const assignmentsApi = {
    assign: (data: AssignUserRequest) =>
        client.post('/user-assignments', data).then((r) => r.data),

    unassign: (data: AssignUserRequest) =>
        client.delete('/user-assignments', { data }).then((r) => r.data),

    getUserAssignments: (userEmail: string) =>
        client
            .get<string[]>(`/user-assignments/${encodeURIComponent(userEmail)}`)
            .then((r) => r.data),
};
