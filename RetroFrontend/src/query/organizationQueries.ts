import type { QueryClient } from '@tanstack/react-query';

const organizationScopedQueryKeys = new Set([
    'organizations',
    'users',
    'retrospectives',
    'retrospective',
    'retrospectiveParticipants',
    'retrospectiveUsers',
    'organizationManagers',
]);

export function clearOrganizationQueries(queryClient: QueryClient) {
    queryClient.removeQueries({
        predicate: (query) => organizationScopedQueryKeys.has(String(query.queryKey[0])),
    });
}
