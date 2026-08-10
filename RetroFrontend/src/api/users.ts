import client from './client';
import type { CreateUserRequest, CreateUserResponse, UserSummaryDto } from '../types';

export const usersApi = {
    getAll: () =>
        client.get<UserSummaryDto[]>('/users').then((r) => r.data),

    create: (data: CreateUserRequest) =>
        client.post<CreateUserResponse>('/users', data).then((r) => r.data),

    updateRole: (id: string, role: string) =>
        client.patch<UserSummaryDto>(`/users/${id}/role`, { role }).then((r) => r.data),

    delete: (id: string) =>
        client.delete(`/users/${id}`),
};
