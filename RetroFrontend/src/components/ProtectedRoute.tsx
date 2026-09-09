import { useEffect } from 'react';
import { Navigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../api/users';
import { useAuthStore } from '../store/authStore';

interface Props {
    children: React.ReactNode;
}

export function ProtectedRoute({ children }: Props) {
    const clearAuth = useAuthStore((s) => s.clearAuth);
    const setProfile = useAuthStore((s) => s.setProfile);

    const meQuery = useQuery({
        queryKey: ['users', 'me'],
        queryFn: usersApi.getMe,
        retry: false,
    });

    useEffect(() => {
        if (meQuery.isError) clearAuth();
    }, [meQuery.isError, clearAuth]);

    useEffect(() => {
        if (!meQuery.data) return;
        setProfile({
            nickname: meQuery.data.nickname,
            avatarUrl: meQuery.data.avatarUrl,
            role: meQuery.data.role,
        });
    }, [meQuery.data, setProfile]);

    if (meQuery.isError) {
        return <Navigate to="/login" replace />;
    }

    if (meQuery.isLoading || !meQuery.data) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-slate-100 text-sm text-slate-500">
                Loading…
            </div>
        );
    }

    return <>{children}</>;
}
