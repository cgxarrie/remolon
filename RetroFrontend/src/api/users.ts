import client from './client';
import type { CreateUserRequest, CreateUserResponse, PagedResponse, UserSummaryDto } from '../types';

export const usersApi = {
    getAll: (organizationId: string, page = 1, pageSize = 20) =>
        client.get<PagedResponse<UserSummaryDto>>('/users', {
            params: { organizationId, page, pageSize },
        }).then((r) => r.data),

    create: (data: CreateUserRequest) =>
        client.post<CreateUserResponse>('/users', data).then((r) => r.data),

    updateRole: (id: string, role: string) =>
        client.patch<UserSummaryDto>(`/users/${id}/role`, { role }).then((r) => r.data),

    delete: (id: string) =>
        client.delete(`/users/${id}`),
};
