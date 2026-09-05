import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { RetrospectivesPage } from './pages/RetrospectivesPage';
import { RetrospectiveDetailPage } from './pages/RetrospectiveDetailPage';
import { UsersPage } from './pages/UsersPage';
import { OrganizationsPage } from './pages/OrganizationsPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { OrganizationThemeProvider } from './theme';

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: 1,
            staleTime: 30_000,
        },
    },
});

export default function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <OrganizationThemeProvider>
                <BrowserRouter>
                    <Routes>
                    <Route path="/login" element={<LoginPage />} />
                    <Route path="/register" element={<RegisterPage />} />
                    <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                    <Route path="/reset-password" element={<ResetPasswordPage />} />
                    <Route
                        path="/"
                        element={<Navigate to="/retrospectives" replace />}
                    />
                    <Route
                        path="/retrospectives"
                        element={
                            <ProtectedRoute>
                                <RetrospectivesPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/retrospectives/:id"
                        element={
                            <ProtectedRoute>
                                <RetrospectiveDetailPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/users"
                        element={
                            <ProtectedRoute>
                                <UsersPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/organizations"
                        element={
                            <ProtectedRoute>
                                <OrganizationsPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route path="*" element={<Navigate to="/" replace />} />
                    </Routes>
                </BrowserRouter>
            </OrganizationThemeProvider>
        </QueryClientProvider>
    );
}
