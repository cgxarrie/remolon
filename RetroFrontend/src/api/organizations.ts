import client from './client';
import type { Organization, PagedResponse } from '../types';

export const organizationsApi = {
    getAll: (page = 1, pageSize = 20) =>
        client.get<PagedResponse<Organization>>('/organizations', {
            params: { page, pageSize },
        }).then((r) => r.data),
    create: (name: string) =>
        client.post<Organization>('/organizations', { name }).then((r) => r.data),
    update: (id: string, name: string) =>
        client.put<Organization>(`/organizations/${id}`, { name }).then((r) => r.data),
    delete: (id: string) => client.delete(`/organizations/${id}`),
};
