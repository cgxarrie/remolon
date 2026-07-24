import client from './client';
import type { AuthTokenResponse, LoginRequest, RegisterRequest } from '../types';

export const authApi = {
    login: (data: LoginRequest) =>
        client.post<AuthTokenResponse>('/auth/login', data).then((r) => r.data),

    register: (data: RegisterRequest) =>
        client.post<AuthTokenResponse>('/auth/register', data).then((r) => r.data),
};
