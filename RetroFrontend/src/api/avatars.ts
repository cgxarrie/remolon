import client from './client';
import type { AvatarUploadResponse } from '../types';

export const avatarsApi = {
    upload: (file: File) => {
        const data = new FormData();
        data.append('file', file);
        return client.post<AvatarUploadResponse>('/users/me/avatar', data).then((r) => r.data);
    },

    get: (userId: string) =>
        client.get<Blob>(`/users/${userId}/avatar`, { responseType: 'blob' }).then((r) => r.data),

    remove: () => client.delete('/users/me/avatar'),
};
