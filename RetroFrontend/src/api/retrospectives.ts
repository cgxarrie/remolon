import client from './client';
import type {
    CreateRetrospectiveRequest,
    GetRetrospectiveDto,
    GetRetrospectiveSummaryDto,
    UpdateRetrospectiveRequest,
} from '../types';

export const retrospectivesApi = {
    getAll: () =>
        client.get<GetRetrospectiveSummaryDto[]>('/retrospectives').then((r) => r.data),

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

    reveal: (id: string) =>
        client.post<string>(`/retrospectives/${id}/reveal`).then((r) => r.data),
};
