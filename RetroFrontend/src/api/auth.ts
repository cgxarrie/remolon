import client from './client';
import type {
    AuthTokenResponse,
    ForgotPasswordRequest,
    ForgotPasswordResponse,
    InitialPasswordChangeRequest,
    LoginRequest,
    RegisterRequest,
    ResetPasswordRequest,
    ChangePasswordRequest,
} from '../types';

export const authApi = {
    login: (data: LoginRequest) =>
        client.post<AuthTokenResponse>('/auth/login', data).then((r) => r.data),

    forgotPassword: (data: ForgotPasswordRequest) =>
        client.post<ForgotPasswordResponse>('/auth/forgot-password', data).then((r) => r.data),

    resetPassword: (data: ResetPasswordRequest) =>
        client.post('/auth/reset-password', data).then((r) => r.data),

    changeInitialPassword: (data: InitialPasswordChangeRequest) =>
        client.post<AuthTokenResponse>('/auth/change-initial-password', data).then((r) => r.data),

    changePassword: (data: ChangePasswordRequest) =>
        client.post('/auth/change-password', data).then((r) => r.data),

    register: (data: RegisterRequest) =>
        client.post<AuthTokenResponse>('/auth/register', data).then((r) => r.data),

    refresh: (refreshToken: string) =>
        client.post<AuthTokenResponse>('/auth/refresh', { refreshToken }).then((r) => r.data),

    logout: (refreshToken: string) =>
        client.post('/auth/logout', { refreshToken }).then((r) => r.data),
};
