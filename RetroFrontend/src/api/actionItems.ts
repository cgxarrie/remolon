import client from './client';
import type {
    CreateActionItemRequest,
    GetActionItemDto,
    UpdateActionItemRequest,
} from '../types';

export const actionItemsApi = {
    create: (data: CreateActionItemRequest) =>
        client.post<string>('/actionitems', data).then((r) => r.data),

    update: (id: string, data: UpdateActionItemRequest) =>
        client.put<GetActionItemDto>(`/actionitems/${id}`, data).then((r) => r.data),

    close: (id: string) =>
        client.post<GetActionItemDto>(`/actionitems/${id}/close`).then((r) => r.data),

    delete: (id: string) => client.delete(`/actionitems/${id}`),
};
