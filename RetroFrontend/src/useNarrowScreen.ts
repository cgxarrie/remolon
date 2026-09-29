import { useEffect, useState } from 'react';

// Matches Tailwind's default `md` breakpoint (min-width: 768px).
const NARROW_SCREEN_QUERY = '(max-width: 767px)';

export function useNarrowScreen() {
    const [matches, setMatches] = useState(() => window.matchMedia(NARROW_SCREEN_QUERY).matches);

    useEffect(() => {
        const media = window.matchMedia(NARROW_SCREEN_QUERY);
        const onChange = () => setMatches(media.matches);
        onChange();
        media.addEventListener('change', onChange);
        return () => media.removeEventListener('change', onChange);
    }, []);

    return matches;
}
