import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr';

export interface ObjectThrownEvent {
    fromUserId: string;
    targetUserId: string;
    objectId: string;
}

export interface ItemsChangedEvent {
    retrospectiveId: string;
}

export type RetrospectiveRevealedEvent = ItemsChangedEvent;
export type RetrospectiveClosedEvent = ItemsChangedEvent;
export type RetrospectiveDeletedEvent = ItemsChangedEvent;
export type RetrospectiveAccessRevokedEvent = ItemsChangedEvent;

export function createRetrospectiveHubConnection(): HubConnection {
    return new HubConnectionBuilder()
        .withUrl('/hubs/retrospective', {
            withCredentials: true,
        })
        .withAutomaticReconnect()
        .build();
}
