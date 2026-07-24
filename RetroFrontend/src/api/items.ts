import client from './client';
import type { CreateItemRequest, GetItemDto, UpdateItemRequest } from '../types';

export const itemsApi = {
    create: (data: CreateItemRequest) =>
        client.post<string>('/items', data).then((r) => r.data),

    update: (id: string, data: UpdateItemRequest) =>
        client.put<GetItemDto>(`/items/${id}`, data).then((r) => r.data),

    delete: (id: string) => client.delete(`/items/${id}`),

    merge: (sourceId: string, targetId: string) =>
        client
            .post<GetItemDto[]>(`/items/${sourceId}/merge`, { targetItemId: targetId })
            .then((r) => r.data),

    unlinkFromGroup: (id: string) =>
        client.delete<GetItemDto>(`/items/${id}/group`).then((r) => r.data),
};
