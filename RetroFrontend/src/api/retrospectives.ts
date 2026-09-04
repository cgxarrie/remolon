import client from './client';
import type {
    CreateRetrospectiveRequest,
    GetRetrospectiveDto,
    GetRetrospectiveSummaryDto,
    UpdateRetrospectiveRequest,
    PagedResponse,
} from '../types';

export const retrospectivesApi = {
    getAll: (organizationId: string, page = 1, pageSize = 20) =>
        client.get<PagedResponse<GetRetrospectiveSummaryDto>>('/retrospectives', {
            params: { organizationId, page, pageSize },
        }).then((r) => r.data),

    getById: (id: string) =>
        client.get<GetRetrospectiveDto>(`/retrospectives/${id}`).then((r) => r.data),

    create: (data: CreateRetrospectiveRequest) =>
        client.post<string>('/retrospectives', data).then((r) => r.data),

    update: (id: string, data: UpdateRetrospectiveRequest) =>
        client.patch<string>(`/retrospectives/${id}`, data).then((r) => r.data),

    delete: (id: string) => client.delete(`/retrospectives/${id}`),

    close: (id: string) =>
        client
            .post<string>(`/retrospectives/${id}/close`, { currentUser: '' })
            .then((r) => r.data),

    createNextIteration: (id: string) =>
        client.post<string>(`/retrospectives/${id}/next-iteration`).then((r) => r.data),

    reveal: (id: string) =>
        client.post<string>(`/retrospectives/${id}/reveal`).then((r) => r.data),
};
