import { Layout } from '../components/Layout';

const sections = [
    { id: 'getting-started', title: 'Getting started' },
    { id: 'roles', title: 'Roles' },
    { id: 'navigation', title: 'Finding your way around' },
    { id: 'boards', title: 'Boards and sessions' },
    { id: 'running', title: 'Running a retrospective' },
    { id: 'feedback', title: 'Adding feedback' },
    { id: 'reveal', title: 'Reveal, merge, and action items' },
    { id: 'close', title: 'Closing and next iteration' },
    { id: 'users', title: 'Inviting and managing users' },
    { id: 'organization', title: 'Organization and theme' },
    { id: 'profile', title: 'Your profile' },
    { id: 'live', title: 'Live collaboration' },
] as const;

function GuideSection({
    id,
    title,
    children,
}: {
    id: string;
    title: string;
    children: React.ReactNode;
}) {
    return (
        <section id={id} className="scroll-mt-6 bg-white dark:bg-slate-900 rounded-xl shadow border border-slate-200 dark:border-slate-700 p-5 space-y-3">
            <h2 className="text-lg font-semibold">{title}</h2>
            <div className="space-y-3 text-sm text-slate-700 dark:text-slate-200 leading-relaxed">{children}</div>
        </section>
    );
}

export function UserGuidePage() {
    return (
        <Layout>
            <div className="max-w-3xl mx-auto space-y-6">
                <header className="space-y-2">
                    <h1 className="text-2xl font-bold">User guide</h1>
                    <p className="text-slate-600 dark:text-slate-300">
                        ReMolon is a retrospective board for teams. Use it to collect feedback in private,
                        reveal it together, turn it into action items, and carry unfinished work into the next
                        session.
                    </p>
                </header>

                <nav aria-label="Guide contents" className="bg-white dark:bg-slate-900 rounded-xl shadow border border-slate-200 dark:border-slate-700 p-4">
                    <h2 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide mb-2">Contents</h2>
                    <ol
                        className="grid grid-cols-1 sm:grid-cols-2 sm:grid-flow-col gap-x-6 gap-y-1 text-sm"
                        style={{ gridTemplateRows: `repeat(${Math.ceil(sections.length / 2)}, auto)` }}
                    >
                        {sections.map((section, index) => (
                            <li key={section.id}>
                                <a href={`#${section.id}`} className="theme-link">
                                    {index + 1}. {section.title}
                                </a>
                            </li>
                        ))}
                    </ol>
                </nav>

                <GuideSection id="getting-started" title="Getting started">
                    <p>
                        There are two ways onto a board. The first person in an organization registers and
                        becomes a Manager. Everyone else is invited by email.
                    </p>
                    <ul className="list-disc pl-5 space-y-1">
                        <li>
                            <strong>Create an organization:</strong> open Register, enter your email, password,
                            and organization name (nickname is optional). You are signed in as a Manager.
                        </li>
                        <li>
                            <strong>Join an existing organization:</strong> a Manager creates your account on the
                            Users page. You receive an invitation link, set a password, and sign in as a Standard
                            User.
                        </li>
                        <li>
                            Forgot your password? Use Forgot password on the login screen. The reset link expires
                            after 30 minutes. Invitation links expire after 30 days.
                        </li>
                    </ul>
                </GuideSection>

                <GuideSection id="roles" title="Roles">
                    <p>Each person has one role in the organization.</p>
                    <div className="overflow-x-auto">
                        <table className="w-full text-sm border-collapse">
                            <thead>
                                <tr className="text-left border-b">
                                    <th className="py-2 pr-3">Capability</th>
                                    <th className="py-2 pr-3">Manager</th>
                                    <th className="py-2">Standard User</th>
                                </tr>
                            </thead>
                            <tbody className="align-top">
                                <tr className="border-b">
                                    <td className="py-2 pr-3">Create boards, invite people, set the organization theme</td>
                                    <td className="py-2 pr-3">Yes</td>
                                    <td className="py-2">No</td>
                                </tr>
                                <tr className="border-b">
                                    <td className="py-2 pr-3">Assign participants, reveal, close, start the next iteration</td>
                                    <td className="py-2 pr-3">Yes</td>
                                    <td className="py-2">No</td>
                                </tr>
                                <tr className="border-b">
                                    <td className="py-2 pr-3">Add, edit, and delete your own feedback items</td>
                                    <td className="py-2 pr-3">Yes</td>
                                    <td className="py-2">Yes, on boards you are assigned to</td>
                                </tr>
                                <tr>
                                    <td className="py-2 pr-3">Merge items after reveal</td>
                                    <td className="py-2 pr-3">Yes</td>
                                    <td className="py-2">No</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </GuideSection>

                <GuideSection id="navigation" title="Finding your way around">
                    <p>
                        After you sign in, the header shows your organization name. Open the menu (the three
                        lines on the left) for Boards, this User guide, and — if you are a Manager — Users and
                        Organization.
                    </p>
                    <p>
                        Click your nickname and avatar on the right to open Profile. Use the sun/moon button
                        beside it to switch light, dark, or system appearance. Your choice is remembered in this
                        browser. The logout button signs you out of this browser.
                    </p>
                </GuideSection>

                <GuideSection id="boards" title="Boards and sessions">
                    <p>
                        A <strong>board</strong> is a named series (for example “Sprint 42”). Each run of that
                        series is a <strong>session</strong> with its own date and Open or Closed status.
                    </p>
                    <ul className="list-disc pl-5 space-y-1">
                        <li>On Boards, expand a title to see every session.</li>
                        <li>Open a session to work on that board.</li>
                        <li>
                            Managers create a board with <strong>+ New Retrospective</strong>. Default columns are
                            “What went well”, “What could be improved”, and “What confused me”. You can rename,
                            add, or remove columns, and pick which Managers own the board.
                        </li>
                    </ul>
                </GuideSection>

                <GuideSection id="running" title="Running a retrospective">
                    <p>A typical Manager flow:</p>
                    <ol className="list-decimal pl-5 space-y-1">
                        <li>Create the board and open it.</li>
                        <li>
                            Click <strong>Participants</strong> and assign the people who should see and write on
                            the board.
                        </li>
                        <li>Let the team add items. Others’ cards stay hidden until you reveal.</li>
                        <li>
                            When everyone is ready, click <strong>Reveal</strong>. All items appear. You can merge
                            similar cards, and the Action Items column becomes available.
                        </li>
                        <li>
                            Capture follow-ups, then click <strong>Close</strong> when the session is finished.
                        </li>
                    </ol>
                    <p>
                        Standard Users only see boards they are assigned to. Add your notes, wait for reveal, then
                        help with action items.
                    </p>
                </GuideSection>

                <GuideSection id="feedback" title="Adding feedback">
                    <ul className="list-disc pl-5 space-y-1">
                        <li>Use <strong>+ Add item</strong> at the bottom of a column.</li>
                        <li>You can edit or delete your own items while the session is open.</li>
                        <li>Drag a card to reorder it in the column or move it to another column.</li>
                        <li>
                            Before reveal, you see your cards. A summary at the top of each column shows how many
                            items other people have added, without showing their notes.
                        </li>
                        <li>Managers can add extra columns on an open board and change a column’s title or color.</li>
                    </ul>
                </GuideSection>

                <GuideSection id="reveal" title="Reveal, merge, and action items">
                    <p>
                        <strong>Reveal</strong> is the moment the room sees everyone’s notes. After reveal:
                    </p>
                    <ul className="list-disc pl-5 space-y-1">
                        <li>All feedback items are visible to participants.</li>
                        <li>
                            Managers can merge related cards by dragging one onto another (or by using merge
                            mode). A merged group can be unlinked later.
                        </li>
                        <li>
                            The <strong>Action Items</strong> column appears. Add follow-ups, assign people (or
                            “all”), and mark items complete.
                        </li>
                        <li>
                            Unfinished work from earlier sessions may show in <strong>Pending Action Items</strong>
                            at the top of the board.
                        </li>
                    </ul>
                </GuideSection>

                <GuideSection id="close" title="Closing and next iteration">
                    <p>
                        Closing locks the session: items can no longer be changed. Assignees can receive an email
                        summary of pending and action items.
                    </p>
                    <p>
                        Closing may create the next session automatically. If it does not, Managers can choose{' '}
                        <strong>Start next iteration</strong> from the closed board or from the Boards list. Open
                        action items and pending items carry forward so the next run starts with leftover work.
                    </p>
                </GuideSection>

                <GuideSection id="users" title="Inviting and managing users">
                    <p>Managers open Users from the menu.</p>
                    <ul className="list-disc pl-5 space-y-1">
                        <li>Enter an email and create the user. ReMolon sends an invitation to set a password.</li>
                        <li>Change someone’s role between Manager and Standard User from the table.</li>
                        <li>Delete a user who should no longer have access. You cannot delete your own account here.</li>
                    </ul>
                </GuideSection>

                <GuideSection id="organization" title="Organization and theme">
                    <p>
                        Managers open Organization from the menu to rename the workspace and pick a theme. Choose
                        a preset or Custom, then save. Header, buttons, and links update for everyone in the
                        organization.
                    </p>
                </GuideSection>

                <GuideSection id="profile" title="Your profile">
                    <p>Open Profile from your name and avatar in the header.</p>
                    <ul className="list-disc pl-5 space-y-1">
                        <li>Nickname is what others see on the board.</li>
                        <li>Choose light, dark, or system appearance. It is saved in this browser.</li>
                        <li>Change your password when you are already signed in.</li>
                        <li>Upload or remove an avatar. It appears on boards and in the header.</li>
                    </ul>
                </GuideSection>

                <GuideSection id="live" title="Live collaboration">
                    <p>
                        While a board is open, changes from other participants appear without a refresh: new
                        items, reveal, close, and participant updates.
                    </p>
                    <p>
                        Participant avatars sit around the board. Click another person’s avatar to throw a fun
                        object their way. Everyone on that board sees it. Use the throw menu to pick which object
                        you send.
                    </p>
                </GuideSection>
            </div>
        </Layout>
    );
}
