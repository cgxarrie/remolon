import { useEffect, useState } from 'react';
import { avatarsApi } from '../api/avatars';

function initialsFrom(name: string): string {
    const parts = name
        .trim()
        .split(/\s+/)
        .filter(Boolean);

    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function colorClassFrom(id: string): string {
    const classes = [
        'bg-rose-100 text-rose-700',
        'bg-amber-100 text-amber-700',
        'bg-emerald-100 text-emerald-700',
        'bg-cyan-100 text-cyan-700',
        'bg-indigo-100 text-indigo-700',
    ];

    let sum = 0;
    for (let i = 0; i < id.length; i += 1) sum += id.charCodeAt(i);
    return classes[sum % classes.length];
}

export function UserAvatar({
    userId,
    avatarUrl,
    name,
    className = 'w-9 h-9 text-xs border border-white/40',
    fallbackClassName,
}: {
    userId: string | null;
    avatarUrl: string | null;
    name: string;
    className?: string;
    fallbackClassName?: string;
}) {
    const [objectUrl, setObjectUrl] = useState<string | null>(null);

    useEffect(() => {
        if (!userId || !avatarUrl) {
            setObjectUrl(null);
            return;
        }

        let loadedUrl: string | undefined;
        let cancelled = false;

        avatarsApi
            .get(avatarUrl)
            .then((blob) => {
                if (cancelled) return;
                loadedUrl = URL.createObjectURL(blob);
                setObjectUrl(loadedUrl);
            })
            .catch(() => {
                if (!cancelled) setObjectUrl(null);
            });

        return () => {
            cancelled = true;
            if (loadedUrl) URL.revokeObjectURL(loadedUrl);
        };
    }, [userId, avatarUrl]);

    const shared = `rounded-full ${className}`;

    if (objectUrl) {
        return <img src={objectUrl} alt="" className={`object-cover ${shared}`} />;
    }

    return (
        <span
            className={`flex items-center justify-center font-semibold ${fallbackClassName ?? colorClassFrom(userId ?? name)} ${shared}`}
            aria-hidden="true"
        >
            {initialsFrom(name)}
        </span>
    );
}
