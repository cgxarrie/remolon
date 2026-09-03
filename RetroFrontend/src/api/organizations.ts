import client from './client';
import type { Organization, PagedResponse, SaveOrganizationRequest } from '../types';

export const organizationsApi = {
    getAll: (page = 1, pageSize = 20) =>
        client.get<PagedResponse<Organization>>('/organizations', {
            params: { page, pageSize },
        }).then((r) => r.data),
    getById: (id: string) =>
        client.get<Organization>(`/organizations/${id}`).then((r) => r.data),
    create: (request: SaveOrganizationRequest) =>
        client.post<Organization>('/organizations', request).then((r) => r.data),
    update: (id: string, request: SaveOrganizationRequest) =>
        client.put<Organization>(`/organizations/${id}`, request).then((r) => r.data),
    delete: (id: string) => client.delete(`/organizations/${id}`),
};
