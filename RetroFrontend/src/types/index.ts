// Auth
export interface AuthTokenResponse {
    token: string;
    email: string;
    role: string;
    nickname: string;
    organizationId?: string;
    organizationName?: string | null;
}

export interface RegisterRequest {
    email: string;
    password: string;
    nickname?: string;
    organizationName: string;
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
    organizationId: string;
    organizationName: string;
    title: string;
    isClosed: boolean;
    isRevealed: boolean;
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

export interface ColumnAuthorCountDto {
    createdBy: string;
    createdByNickname: string;
    count: number;
}

export interface GetColumnDto {
    id: string;
    title: string;
    position: number;
    headerColor: string | null;
    items: GetItemDto[];
    hiddenAuthorCounts: ColumnAuthorCountDto[];
}

export interface GetActionColumnDto {
    id: string;
    title: string;
    position: number;
    items: GetActionItemDto[];
}

export interface GetRetrospectiveDto {
    id: string;
    organizationId: string;
    organizationName: string;
    title: string;
    isClosed: boolean;
    canStartNextIteration: boolean;
    isRevealed: boolean;
    retrospectiveDate: string | null;
    columns: GetColumnDto[];
    actionColumns: GetActionColumnDto[];
    createdAt: string;
    updatedAt: string;
}

export interface CreateRetrospectiveRequest {
    title: string;
    organizationId?: string;
    managerUserIds: string[];
    columns: { title: string; position: number }[];
}

export interface UpdateRetrospectiveRequest {
    title?: string;
    addColumns?: { title: string; position: number }[];
    updateColumns?: { id: string; title?: string; position?: number; headerColor?: string }[];
    removeColumnIds?: string[];
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

export interface BatchAssignUsersRequest {
    retrospectiveId: string;
    userIds: string[];
}

export interface BatchAssignUsersResponse {
    assignedCount: number;
    removedCount: number;
}

export type Role = 'Admin' | 'Manager' | 'StandardUser';

export interface UserSummaryDto {
    id: string;
    email: string;
    nickname: string;
    role: Role;
    organizationId: string | null;
    organizationName: string | null;
}

export interface CreateUserRequest {
    email: string;
    nickname?: string;
    role?: Role;
    organizationId?: string;
}

// The identity of an organization without its theme, which is loaded separately by
// OrganizationThemeProvider and is not needed by pages that only scope queries by organization.
export interface SelectedOrganization {
    id: string;
    name: string;
}

export interface Organization extends SelectedOrganization {
    theme: OrganizationTheme;
}

export type ThemeKey = 'default' | 'ocean' | 'forest' | 'sunset' | 'custom';

export interface OrganizationTheme {
    themeKey: ThemeKey;
    headerColor: string | null;
    headerHoverColor: string | null;
    accentColor: string | null;
    accentHoverColor: string | null;
    focusColor: string | null;
}

export interface SaveOrganizationRequest {
    name: string;
    themeKey: ThemeKey;
    headerColor?: string | null;
    headerHoverColor?: string | null;
    accentColor?: string | null;
    accentHoverColor?: string | null;
    focusColor?: string | null;
}

export interface PagedResponse<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
}

export interface CreateUserResponse {
    id: string;
    email: string;
    nickname: string;
    role: Role;
    invitationEmailSent: boolean;
    temporaryPassword?: string | null;
}
