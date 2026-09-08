import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { organizationsApi } from '../api/organizations';
import { usersApi } from '../api/users';
import { UserAvatar } from './UserAvatar';
import { clearOrganizationQueries } from '../query/organizationQueries';
import { useAuthStore } from '../store/authStore';

const iconProps = {
    xmlns: 'http://www.w3.org/2000/svg',
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.75,
    strokeLinecap: 'round',
    strokeLinejoin: 'round',
    className: 'w-4 h-4',
    'aria-hidden': true,
} as const;

const usersIcon = (
    <svg {...iconProps}>
        <path d="M16 19v-1a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v1" />
        <circle cx="9" cy="7" r="4" />
        <path d="M22 19v-1a4 4 0 0 0-3-3.87" />
        <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
);

const organizationsIcon = (
    <svg {...iconProps}>
        <path d="M3 21h18" />
        <path d="M5 21V5a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v16" />
        <path d="M15 21V11h2a2 2 0 0 1 2 2v8" />
        <path d="M9 7h2" />
        <path d="M9 11h2" />
        <path d="M9 15h2" />
    </svg>
);

export function Layout({ children }: { children: React.ReactNode }) {
    const {
        email,
        nickname,
        role,
        userId,
        avatarUrl,
        organizationId,
        organizationName,
        setOrganizationName,
        setProfile,
        clearAuth,
    } = useAuthStore();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [menuOpen, setMenuOpen] = useState(false);
    const menuRef = useRef<HTMLDivElement | null>(null);

    // Sessions persisted before the organization name was part of the auth response
    // still need a one-time fetch of the current organization.
    const needsOrganizationName = role !== null && !organizationName && !!organizationId;
    const { data: me } = useQuery({
        queryKey: ['users', 'me'],
        queryFn: () => usersApi.getMe(),
        staleTime: 30_000,
    });

    useEffect(() => {
        if (!me) return;
        setProfile({ nickname: me.nickname, avatarUrl: me.avatarUrl, role: me.role });
    }, [me, setProfile]);

    const { data: ownOrganizations } = useQuery({
        queryKey: ['organizations', 'current', organizationId],
        queryFn: () => organizationsApi.getAll(1, 1),
        enabled: needsOrganizationName,
        staleTime: Infinity,
    });

    useEffect(() => {
        const name = ownOrganizations?.items[0]?.name;
        if (name && name !== organizationName) setOrganizationName(name);
    }, [ownOrganizations, organizationName, setOrganizationName]);

    useEffect(() => {
        if (!menuOpen) return;

        function handlePointerDown(event: MouseEvent) {
            if (!menuRef.current?.contains(event.target as Node)) setMenuOpen(false);
        }

        function handleEscape(event: KeyboardEvent) {
            if (event.key === 'Escape') setMenuOpen(false);
        }

        window.addEventListener('mousedown', handlePointerDown);
        window.addEventListener('keydown', handleEscape);

        return () => {
            window.removeEventListener('mousedown', handlePointerDown);
            window.removeEventListener('keydown', handleEscape);
        };
    }, [menuOpen]);

    function handleLogout() {
        clearOrganizationQueries(queryClient);
        clearAuth();
        navigate('/login');
    }

    const menuItems = [
        { to: '/users', label: 'Users', visible: role === 'Manager', icon: usersIcon },
        {
            to: '/organizations',
            label: 'Organization',
            visible: role === 'Manager',
            icon: organizationsIcon,
        },
    ].filter((item) => item.visible);

    const homePath = '/retrospectives';
    const brand = organizationName ? `ReMolon - ${organizationName}` : 'ReMolon';
    // Sessions persisted before the nickname was stored fall back to the email.
    const displayName = nickname ?? email;

    const singleOrganizationName = role !== null ? organizationName : null;
    useEffect(() => {
        document.title = singleOrganizationName ? `ReMolon - ${singleOrganizationName}` : 'ReMolon';
        return () => {
            document.title = 'ReMolon';
        };
    }, [singleOrganizationName]);

    return (
        <div className="min-h-screen flex flex-col">
            <header className="theme-header text-white shadow-md">
                <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        {menuItems.length > 0 && (
                            <div ref={menuRef} className="relative">
                                <button
                                    onClick={() => setMenuOpen((open) => !open)}
                                    aria-label="Menu"
                                    aria-expanded={menuOpen}
                                    aria-haspopup="menu"
                                    title="Menu"
                                    className="p-1.5 theme-header-hover rounded-md transition-colors"
                                >
                                    <svg
                                        xmlns="http://www.w3.org/2000/svg"
                                        viewBox="0 0 24 24"
                                        fill="none"
                                        stroke="currentColor"
                                        strokeWidth={1.75}
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        className="w-5 h-5"
                                        aria-hidden="true"
                                    >
                                        <path d="M4 6h16" />
                                        <path d="M4 12h16" />
                                        <path d="M4 18h16" />
                                    </svg>
                                </button>
                                {menuOpen && (
                                    <div
                                        role="menu"
                                        className="absolute left-0 top-full mt-2 min-w-[12rem] z-20 theme-header border border-white/25 rounded-md shadow-lg py-1 text-sm"
                                    >
                                        {menuItems.map((item) => (
                                            <Link
                                                key={item.to}
                                                to={item.to}
                                                role="menuitem"
                                                onClick={() => setMenuOpen(false)}
                                                className="flex items-center gap-2 px-3 py-2 hover:bg-white/10 transition-colors"
                                            >
                                                {item.icon}
                                                <span>{item.label}</span>
                                            </Link>
                                        ))}
                                    </div>
                                )}
                            </div>
                        )}
                        <Link to={homePath} className="text-xl font-bold tracking-tight hover:opacity-80">
                            {brand}
                        </Link>
                    </div>
                    <div className="flex items-center gap-4 text-sm">
                        <Link
                            to="/profile"
                            className="flex items-center gap-3 theme-header-hover rounded-md px-2 py-1 transition-colors"
                            aria-label="Edit profile"
                            title="Edit profile"
                        >
                            <div className="flex flex-col items-end leading-tight pl-4 border-l border-white/25">
                                <span className="opacity-90">{displayName}</span>
                                <span className="text-xs font-medium opacity-75">{role}</span>
                            </div>
                            <UserAvatar userId={userId} avatarUrl={avatarUrl} name={displayName ?? ''} />
                        </Link>
                        <button
                            onClick={handleLogout}
                            aria-label="Logout"
                            title="Logout"
                            className="p-1.5 theme-header-hover rounded-md transition-colors"
                        >
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth={1.75}
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                className="w-5 h-5"
                                aria-hidden="true"
                            >
                                <path d="M15 3h3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-3" />
                                <path d="M10 17l-5-5 5-5" />
                                <path d="M15 12H5" />
                            </svg>
                        </button>
                    </div>
                </div>
            </header>
            <main className="flex-1 max-w-7xl w-full mx-auto px-4 py-6">{children}</main>
        </div>
    );
}
