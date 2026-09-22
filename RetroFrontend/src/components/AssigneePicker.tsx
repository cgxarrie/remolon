interface Props {
    options: string[];
    selected: string[];
    onChange: (next: string[]) => void;
}

export function AssigneePicker({ options, selected, onChange }: Props) {
    const hasAll = selected.includes('all');

    function toggleAll() {
        onChange(hasAll ? [] : ['all']);
    }

    function togglePerson(name: string) {
        if (hasAll) {
            onChange([name]);
            return;
        }

        if (selected.includes(name)) {
            onChange(selected.filter((current) => current !== name));
            return;
        }

        onChange([...selected, name]);
    }

    return (
        <div className="max-h-36 overflow-y-auto rounded border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-900 px-2 py-1.5 space-y-1">
            <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
                <input
                    type="checkbox"
                    checked={hasAll}
                    onChange={toggleAll}
                    className="rounded border-slate-300 dark:border-slate-600 text-indigo-600 dark:text-indigo-400 focus:ring-indigo-500"
                />
                all
            </label>
            {options.map((name) => (
                <label key={name} className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
                    <input
                        type="checkbox"
                        checked={!hasAll && selected.includes(name)}
                        onChange={() => togglePerson(name)}
                        className="rounded border-slate-300 dark:border-slate-600 text-indigo-600 dark:text-indigo-400 focus:ring-indigo-500"
                    />
                    {name}
                </label>
            ))}
        </div>
    );
}
