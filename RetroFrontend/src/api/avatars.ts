import client from './client';
import type { AvatarUploadResponse } from '../types';

export const avatarsApi = {
    upload: (file: File) => {
        const data = new FormData();
        data.append('file', file);
        return client.post<AvatarUploadResponse>('/users/me/avatar', data).then((r) => r.data);
    },

    get: (avatarUrl: string) =>
        client
            .get<Blob>(avatarUrl.startsWith('/api/') ? avatarUrl.slice('/api'.length) : avatarUrl, {
                responseType: 'blob',
            })
            .then((r) => r.data),

    remove: () => client.delete('/users/me/avatar'),
};
