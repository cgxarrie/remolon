import { Link } from 'react-router-dom';
import { ColorModeToggle } from '../components/ColorModeToggle';
import { useAuthStore } from '../store/authStore';

const features = [
    {
        title: 'Private until reveal',
        body: 'Everyone writes their own notes first. Others’ cards stay hidden until a Manager reveals the board, so people can be honest before the group discussion starts.',
    },
    {
        title: 'Live collaboration',
        body: 'New items, reveals, closes, and participant changes show up for everyone on the board without a refresh.',
    },
    {
        title: 'Merge and rearrange',
        body: 'After reveal, Managers can merge related cards, and anyone can drag items to reorder them or move them between columns.',
    },
    {
        title: 'Action items that carry forward',
        body: 'Capture follow-ups, assign people, and mark them done. Unfinished work rolls into the next session so the next retro starts with leftover commitments.',
    },
    {
        title: 'Boards as a series',
        body: 'A board is a named series, like a sprint. Each run is its own session with a date and Open or Closed status, so history stays easy to find.',
    },
    {
        title: 'Organization theming',
        body: 'Managers set the workspace name and a theme. Header, buttons, and links update for everyone in the organization.',
    },
    {
        title: 'Invite by email',
        body: 'The first person registers and becomes a Manager. Everyone else gets an invitation to set a password and join as a Standard User.',
    },
    {
        title: 'Roles that match the room',
        body: 'Managers own boards, people, reveal, and close. Standard Users contribute on the sessions they are assigned to.',
    },
];

const steps = [
    {
        n: '1',
        title: 'Create your organization',
        body: 'Register with an email, password, and team name. You sign in as a Manager and can start a board immediately.',
    },
    {
        n: '2',
        title: 'Invite people and open a session',
        body: 'Add teammates by email, assign who should see the board, and let them write. Default columns cover what went well, what could improve, and what confused people — you can rename or add more.',
    },
    {
        n: '3',
        title: 'Reveal, merge, and act',
        body: 'When the room is ready, reveal everyone’s notes. Group similar cards, fill the Action Items column, then close. The next iteration can start with unfinished work already on the board.',
    },
];

function BoardPreview() {
    return (
        <div
            className="rounded-2xl border border-white/15 bg-white/10 p-4 shadow-2xl backdrop-blur-sm dark:bg-slate-900/50"
            aria-hidden="true"
        >
            <div className="mb-3 flex items-center justify-between text-xs text-indigo-100">
                <span className="font-medium">Sprint 42 · Open</span>
                <span className="rounded-full bg-amber-400/90 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-950">
                    Hidden until reveal
                </span>
            </div>
            <div className="grid grid-cols-3 gap-3">
                {[
                    {
                        title: 'Went well',
                        yours: 'Deploy pipeline is faster',
                        hidden: 4,
                    },
                    {
                        title: 'Improve',
                        yours: 'Standups ran long',
                        hidden: 3,
                    },
                    {
                        title: 'Confused me',
                        yours: 'Who owns the alert?',
                        hidden: 2,
                    },
                ].map((col) => (
                    <div key={col.title} className="rounded-xl bg-white p-3 text-slate-800 shadow-sm dark:bg-slate-800 dark:text-slate-100">
                        <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                            {col.title}
                        </p>
                        <div className="rounded-lg border border-indigo-200 bg-indigo-50 p-2 text-xs dark:border-indigo-500/40 dark:bg-indigo-950/50">
                            {col.yours}
                            <p className="mt-1 text-[10px] text-indigo-600 dark:text-indigo-300">You</p>
                        </div>
                        <p className="mt-2 text-[11px] text-slate-500 dark:text-slate-400">
                            {col.hidden} more hidden
                        </p>
                    </div>
                ))}
            </div>
        </div>
    );
}

export function LandingPage() {
    const userId = useAuthStore((s) => s.userId);
    const signedIn = Boolean(userId);

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
            <header className="sticky top-0 z-20 border-b border-slate-200/80 bg-white/90 backdrop-blur dark:border-slate-800 dark:bg-slate-950/90">
                <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
                    <a href="#top" className="text-lg font-bold text-indigo-700 dark:text-indigo-300">
                        ReMolon
                    </a>
                    <nav className="hidden items-center gap-6 text-sm sm:flex" aria-label="Landing">
                        <a href="#features" className="text-slate-600 hover:text-slate-900 dark:text-slate-300 dark:hover:text-white">
                            Features
                        </a>
                        <a href="#how-it-works" className="text-slate-600 hover:text-slate-900 dark:text-slate-300 dark:hover:text-white">
                            How it works
                        </a>
                        <a href="#roles" className="text-slate-600 hover:text-slate-900 dark:text-slate-300 dark:hover:text-white">
                            Roles
                        </a>
                    </nav>
                    <div className="flex items-center gap-2">
                        <ColorModeToggle />
                        {signedIn ? (
                            <Link
                                to="/retrospectives"
                                className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700"
                            >
                                Open boards
                            </Link>
                        ) : (
                            <>
                                <Link
                                    to="/login"
                                    className="hidden rounded-md px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 sm:inline dark:text-slate-200 dark:hover:bg-slate-800"
                                >
                                    Sign in
                                </Link>
                                <Link
                                    to="/register"
                                    className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700"
                                >
                                    Try ReMolon
                                </Link>
                            </>
                        )}
                    </div>
                </div>
            </header>

            <main id="top">
                <section className="theme-header">
                    <div className="mx-auto grid max-w-6xl items-center gap-10 px-4 py-16 text-white lg:grid-cols-2 lg:py-20">
                        <div className="space-y-6">
                            <p className="text-sm font-semibold uppercase tracking-wider text-indigo-200">
                                Retrospective boards for agile teams
                            </p>
                            <h1 className="text-4xl font-bold leading-tight tracking-tight sm:text-5xl">
                                Write in private. Reveal together. Carry the work forward.
                            </h1>
                            <p className="max-w-xl text-base leading-relaxed text-indigo-100 sm:text-lg">
                                ReMolon is a live retrospective board. Collect notes without groupthink, discuss
                                them after reveal, turn them into action items, and keep unfinished work on the next
                                session.
                            </p>
                            <div className="flex flex-wrap gap-3">
                                {signedIn ? (
                                    <Link
                                        to="/retrospectives"
                                        className="rounded-md bg-white px-5 py-2.5 text-sm font-semibold text-indigo-800 hover:bg-indigo-50"
                                    >
                                        Go to your boards
                                    </Link>
                                ) : (
                                    <>
                                        <Link
                                            to="/register"
                                            className="rounded-md bg-white px-5 py-2.5 text-sm font-semibold text-indigo-800 hover:bg-indigo-50"
                                        >
                                            Create an organization
                                        </Link>
                                        <Link
                                            to="/login"
                                            className="rounded-md border border-white/40 px-5 py-2.5 text-sm font-semibold text-white hover:bg-white/10"
                                        >
                                            Sign in
                                        </Link>
                                    </>
                                )}
                            </div>
                            <p className="text-sm text-indigo-200">
                                Registering creates a Manager account for your team. Invite everyone else by email.
                            </p>
                        </div>
                        <BoardPreview />
                    </div>
                </section>

                <section id="features" className="scroll-mt-16 mx-auto max-w-6xl px-4 py-16">
                    <h2 className="text-2xl font-bold">What you get</h2>
                    <p className="mt-2 max-w-2xl text-slate-600 dark:text-slate-300">
                        Built for the way a real retro runs: quiet writing, a shared reveal, grouping, follow-ups,
                        then the next iteration.
                    </p>
                    <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                        {features.map((feature) => (
                            <article
                                key={feature.title}
                                className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-900"
                            >
                                <h3 className="font-semibold">{feature.title}</h3>
                                <p className="mt-2 text-sm leading-relaxed text-slate-600 dark:text-slate-300">
                                    {feature.body}
                                </p>
                            </article>
                        ))}
                    </div>
                </section>

                <section id="how-it-works" className="scroll-mt-16 border-y border-slate-200 bg-white py-16 dark:border-slate-800 dark:bg-slate-900">
                    <div className="mx-auto max-w-6xl px-4">
                        <h2 className="text-2xl font-bold">How a session works</h2>
                        <ol className="mt-8 grid gap-6 md:grid-cols-3">
                            {steps.map((step) => (
                                <li key={step.n} className="rounded-xl border border-slate-200 p-5 dark:border-slate-700">
                                    <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-indigo-600 text-sm font-bold text-white">
                                        {step.n}
                                    </span>
                                    <h3 className="mt-3 font-semibold">{step.title}</h3>
                                    <p className="mt-2 text-sm leading-relaxed text-slate-600 dark:text-slate-300">
                                        {step.body}
                                    </p>
                                </li>
                            ))}
                        </ol>
                    </div>
                </section>

                <section id="roles" className="scroll-mt-16 mx-auto max-w-6xl px-4 py-16">
                    <h2 className="text-2xl font-bold">Who does what</h2>
                    <div className="mt-8 grid gap-4 md:grid-cols-2">
                        <article className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-900">
                            <h3 className="text-lg font-semibold">Manager</h3>
                            <ul className="mt-3 list-disc space-y-1 pl-5 text-sm text-slate-600 dark:text-slate-300">
                                <li>Create boards, assign participants, reveal and close sessions</li>
                                <li>Start the next iteration and carry open action items forward</li>
                                <li>Invite people, change roles, and set the organization theme</li>
                            </ul>
                        </article>
                        <article className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-900">
                            <h3 className="text-lg font-semibold">Standard User</h3>
                            <ul className="mt-3 list-disc space-y-1 pl-5 text-sm text-slate-600 dark:text-slate-300">
                                <li>Join the sessions you are assigned to</li>
                                <li>Add and edit your own notes while the session is open</li>
                                <li>Help with action items after the board is revealed</li>
                            </ul>
                        </article>
                    </div>
                </section>

                <section className="px-4 pb-16">
                    <div className="mx-auto max-w-6xl rounded-2xl bg-indigo-700 px-6 py-10 text-center text-white sm:px-10">
                        <h2 className="text-2xl font-bold">Ready to run the next retro?</h2>
                        <p className="mx-auto mt-2 max-w-xl text-indigo-100">
                            Create an organization in a minute, invite the team, and open a board. Notes stay
                            private until you reveal them.
                        </p>
                        <div className="mt-6 flex flex-wrap justify-center gap-3">
                            {signedIn ? (
                                <Link
                                    to="/retrospectives"
                                    className="rounded-md bg-white px-5 py-2.5 text-sm font-semibold text-indigo-800 hover:bg-indigo-50"
                                >
                                    Open boards
                                </Link>
                            ) : (
                                <>
                                    <Link
                                        to="/register"
                                        className="rounded-md bg-white px-5 py-2.5 text-sm font-semibold text-indigo-800 hover:bg-indigo-50"
                                    >
                                        Create an organization
                                    </Link>
                                    <Link
                                        to="/login"
                                        className="rounded-md border border-white/40 px-5 py-2.5 text-sm font-semibold text-white hover:bg-white/10"
                                    >
                                        Sign in
                                    </Link>
                                </>
                            )}
                        </div>
                    </div>
                </section>
            </main>

            <footer className="border-t border-slate-200 py-6 text-center text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400">
                ReMolon · retrospective boards for teams
            </footer>
        </div>
    );
}
