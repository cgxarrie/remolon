import type { QueryClient } from '@tanstack/react-query';

// The detail page caches a retrospective as ['retrospective', organizationId, retroId],
// so matching on the id anywhere in the key keeps callers that only know the retro id working.
export function invalidateRetrospective(queryClient: QueryClient, retroId: string) {
    return queryClient.invalidateQueries({
        predicate: (query) =>
            query.queryKey[0] === 'retrospective' && query.queryKey.includes(retroId),
    });
}
