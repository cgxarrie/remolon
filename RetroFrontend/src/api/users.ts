import client from './client';
import type {
    AuthTokenResponse,
    CreateUserRequest,
    CreateUserResponse,
    CurrentUserDto,
    PagedResponse,
    UpdateMeRequest,
    UserSummaryDto,
} from '../types';

export const usersApi = {
    getMe: () =>
        client.get<CurrentUserDto>('/users/me').then((r) => r.data),

    updateMe: (data: UpdateMeRequest) =>
        client.patch<AuthTokenResponse>('/users/me', data).then((r) => r.data),

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
