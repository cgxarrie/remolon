import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export function Layout({ children }: { children: React.ReactNode }) {
    const { email, role, clearAuth } = useAuthStore();
    const navigate = useNavigate();

    function handleLogout() {
        clearAuth();
        navigate('/login');
    }

    return (
        <div className="min-h-screen flex flex-col">
            <header className="bg-indigo-700 text-white shadow-md">
                <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
                    <Link to="/" className="text-xl font-bold tracking-tight hover:opacity-80">
                        Retro Molon
                    </Link>
                    <div className="flex items-center gap-4 text-sm">
                        {role === 'Admin' && (
                            <Link to="/users" className="hover:opacity-80 transition-opacity">
                                Users
                            </Link>
                        )}
                        <span className="opacity-80">
                            {email}{' '}
                            <span className="ml-1 px-2 py-0.5 bg-indigo-500 rounded-full text-xs font-medium">
                                {role}
                            </span>
                        </span>
                        <button
                            onClick={handleLogout}
                            className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-500 rounded-md transition-colors"
                        >
                            Logout
                        </button>
                    </div>
                </div>
            </header>
            <main className="flex-1 max-w-7xl w-full mx-auto px-4 py-6">{children}</main>
        </div>
    );
}
