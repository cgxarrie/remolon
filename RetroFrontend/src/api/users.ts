import client from './client';
import type { UserSummaryDto } from '../types';

export const usersApi = {
    getAll: () =>
        client.get<UserSummaryDto[]>('/users').then((r) => r.data),

    updateRole: (id: string, role: string) =>
        client.patch<UserSummaryDto>(`/users/${id}/role`, { role }).then((r) => r.data),
};
