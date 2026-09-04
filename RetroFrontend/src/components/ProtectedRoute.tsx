import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

interface Props {
    children: React.ReactNode;
    allowAdminWithoutOrganization?: boolean;
}

export function ProtectedRoute({ children, allowAdminWithoutOrganization = false }: Props) {
    const token = useAuthStore((s) => s.token);
    const role = useAuthStore((s) => s.role);
    const selectedOrganizationId = useAuthStore((s) => s.selectedOrganizationId);
    if (!token) return <Navigate to="/login" replace />;
    if (role === 'Admin' && !selectedOrganizationId && !allowAdminWithoutOrganization) {
        return <Navigate to="/" replace />;
    }
    return <>{children}</>;
}
