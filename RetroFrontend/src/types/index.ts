// Auth
export interface AuthTokenResponse {
    token: string;
    email: string;
    role: string;
    nickname: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
    nickname?: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface ForgotPasswordRequest {
    email: string;
}

export interface ForgotPasswordResponse {
    message: string;
    resetToken: string | null;
}

export interface ResetPasswordRequest {
    email: string;
    token: string;
    newPassword: string;
}

export interface InitialPasswordChangeRequest {
    email: string;
    currentPassword: string;
    newPassword: string;
}

export interface PasswordChangeRequiredResponse {
    message: string;
    requiresPasswordChange: boolean;
}

// Retrospectives
export interface GetRetrospectiveSummaryDto {
    id: string;
    title: string;
    isClosed: boolean;
    retrospectiveDate: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface GetItemDto {
    id: string;
    iterationId: string;
    columnId: string;
    groupId: string | null;
    description: string;
    position: number;
    createdBy: string;
    createdByNickname: string;
    createdAt: string;
    updatedAt: string;
}

export interface GetActionItemDto extends GetItemDto {
    assignee: string;
    isCompleted: boolean;
    iterations: number;
    closedBy: string | null;
    closedAt: string | null;
}

export interface GetColumnDto {
    id: string;
    title: string;
    position: number;
    headerColor: string | null;
    items: GetItemDto[];
}

export interface GetActionColumnDto {
    id: string;
    title: string;
    position: number;
    items: GetActionItemDto[];
}

export interface GetRetrospectiveDto {
    id: string;
    title: string;
    isClosed: boolean;
    retrospectiveDate: string | null;
    columns: GetColumnDto[];
    actionColumns: GetActionColumnDto[];
    createdAt: string;
    updatedAt: string;
}

export interface CreateRetrospectiveRequest {
    title: string;
    columns: { title: string; position: number }[];
}

export interface UpdateRetrospectiveRequest {
    title?: string;
    addColumns?: { title: string; position: number }[];
    updateColumns?: { id: string; title?: string; position?: number; headerColor?: string }[];
    removeColumnIds?: string[];
    retrospectiveDate?: string;
}

export interface CreateItemRequest {
    columnId: string;
    description: string;
    position: number;
}

export interface UpdateItemRequest {
    description?: string;
    position?: number;
}

export interface CreateActionItemRequest {
    columnId: string;
    description: string;
    position: number;
    assignee?: string;
}

export interface UpdateActionItemRequest {
    description?: string;
    position?: number;
    assignee?: string;
    isCompleted?: boolean;
}

export interface AssignUserRequest {
    userEmail: string;
    retrospectiveId: string;
}

export type Role = 'Admin' | 'Manager' | 'StandardUser';

export interface UserSummaryDto {
    id: string;
    email: string;
    nickname: string;
    role: Role;
}

export interface CreateUserRequest {
    email: string;
    nickname?: string;
    role?: Role;
}

export interface CreateUserResponse {
    id: string;
    email: string;
    nickname: string;
    role: Role;
    temporaryPassword: string;
}
