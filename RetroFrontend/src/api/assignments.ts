import client from './client';
import type {
    AssignUserRequest,
    BatchAssignUsersRequest,
    BatchAssignUsersResponse,
    UserSummaryDto,
} from '../types';

export const assignmentsApi = {
    assign: (data: AssignUserRequest) =>
        client.post('/user-assignments', data).then((r) => r.data),

    assignBatch: (data: BatchAssignUsersRequest) =>
        client.post<BatchAssignUsersResponse>('/user-assignments/batch', data).then((r) => r.data),

    unassign: (data: AssignUserRequest) =>
        client.delete('/user-assignments', { data }).then((r) => r.data),

    getUserAssignments: (userEmail: string) =>
        client
            .get<string[]>(`/user-assignments/${encodeURIComponent(userEmail)}`)
            .then((r) => r.data),

    getRetrospectiveParticipants: (retrospectiveId: string) =>
        client
            .get<UserSummaryDto[]>(`/user-assignments/retrospective/${retrospectiveId}/participants`)
            .then((r) => r.data),

    getRetrospectiveUsers: (retrospectiveId: string) =>
        client
            .get<UserSummaryDto[]>(`/user-assignments/retrospective/${retrospectiveId}/users`)
            .then((r) => r.data),
};
