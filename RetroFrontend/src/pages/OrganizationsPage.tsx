import { useEffect, useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { organizationsApi } from '../api/organizations';
import { clearOrganizationQueries } from '../query/organizationQueries';
import { useAuthStore } from '../store/authStore';
import { defaultCustomPalette, resolveTheme, themePresets } from '../theme';
import type { Organization, SaveOrganizationRequest, ThemeKey } from '../types';

const themeOptions: ThemeKey[] = ['default', 'ocean', 'forest', 'sunset', 'custom'];

function emptyDraft(): SaveOrganizationRequest {
    return {
        name: '',
        themeKey: 'default',
        headerColor: defaultCustomPalette.header,
        headerHoverColor: defaultCustomPalette.headerHover,
        accentColor: defaultCustomPalette.accent,
        accentHoverColor: defaultCustomPalette.accentHover,
        focusColor: defaultCustomPalette.focus,
    };
}

function draftFromOrganization(organization: Organization): SaveOrganizationRequest {
    const palette = resolveTheme(organization.theme);
    return {
        name: organization.name,
        themeKey: organization.theme.themeKey,
        headerColor: palette.header,
        headerHoverColor: palette.headerHover,
        accentColor: palette.accent,
        accentHoverColor: palette.accentHover,
        focusColor: palette.focus,
    };
}

function ThemePicker({
    draft,
    onChange,
}: {
    draft: SaveOrganizationRequest;
    onChange: (draft: SaveOrganizationRequest) => void;
}) {
    const customPalette = {
        name: 'Custom',
        header: draft.headerColor ?? defaultCustomPalette.header,
        headerHover: draft.headerHoverColor ?? defaultCustomPalette.headerHover,
        accent: draft.accentColor ?? defaultCustomPalette.accent,
        accentHover: draft.accentHoverColor ?? defaultCustomPalette.accentHover,
        focus: draft.focusColor ?? defaultCustomPalette.focus,
    };

    return (
        <div className="space-y-3">
            <span className="text-sm font-medium text-slate-700">Theme</span>
            <div className="grid grid-cols-2 sm:grid-cols-5 gap-2">
                {themeOptions.map((key) => {
                    const palette = key === 'custom' ? customPalette : themePresets[key];
                    return (
                        <button
                            type="button"
                            key={key}
                            onClick={() => onChange({ ...draft, themeKey: key })}
                            className={`rounded-lg border p-2 text-left transition ${
                                draft.themeKey === key ? 'ring-2 border-transparent' : 'border-slate-200'
                            }`}
                            style={draft.themeKey === key ? { boxShadow: `0 0 0 2px ${palette.focus}` } : undefined}
                        >
                            <span className="flex h-5 overflow-hidden rounded mb-1">
                                <span className="flex-1" style={{ backgroundColor: palette.header }} />
                                <span className="flex-1" style={{ backgroundColor: palette.accent }} />
                                <span className="flex-1" style={{ backgroundColor: palette.focus }} />
                            </span>
                            <span className="text-xs font-medium">{palette.name}</span>
                        </button>
                    );
                })}
            </div>
            {draft.themeKey === 'custom' && (
                <div className="grid sm:grid-cols-5 gap-3 rounded-lg bg-slate-50 p-3">
                    {([
                        ['Header', 'headerColor'],
                        ['Header hover', 'headerHoverColor'],
                        ['Accent', 'accentColor'],
                        ['Accent hover', 'accentHoverColor'],
                        ['Focus', 'focusColor'],
                    ] as const).map(([label, field]) => (
                        <label key={field} className="text-xs text-slate-600">
                            {label}
                            <input
                                type="color"
                                className="mt-1 block h-9 w-full cursor-pointer rounded border border-slate-300"
                                value={draft[field] ?? defaultCustomPalette[field.replace('Color', '') as keyof typeof defaultCustomPalette]}
                                onChange={(event) => onChange({ ...draft, [field]: event.target.value })}
                            />
                        </label>
                    ))}
                </div>
            )}
        </div>
    );
}

export function OrganizationsPage() {
    const {
        role,
        organizationId,
        selectedOrganizationId,
        setOrganizationName,
        setSelectedOrganization,
        clearSelectedOrganization,
    } = useAuthStore();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [page, setPage] = useState(1);
    const [draft, setDraft] = useState<SaveOrganizationRequest>(emptyDraft);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [error, setError] = useState('');
    const isAdmin = role === 'Admin';
    const isManager = role === 'Manager';
    const { data } = useQuery({
        queryKey: ['organizations', page],
        queryFn: () => organizationsApi.getAll(page),
        enabled: isAdmin,
    });
    const { data: managerOrganization, isLoading: isManagerOrganizationLoading } = useQuery({
        queryKey: ['organizationEdit', organizationId],
        queryFn: () => organizationsApi.getById(organizationId!),
        enabled: isManager && !!organizationId,
    });

    useEffect(() => {
        if (!isManager || !managerOrganization) return;
        setDraft(draftFromOrganization(managerOrganization));
        setEditingId(managerOrganization.id);
    }, [isManager, managerOrganization]);

    const refresh = () => queryClient.invalidateQueries({ queryKey: ['organizations'] });
    const mutation = useMutation({
        mutationFn: () => {
            const request = { ...draft, name: draft.name.trim() };
            if (isManager && organizationId) return organizationsApi.update(organizationId, request);
            return editingId
                ? organizationsApi.update(editingId, request)
                : organizationsApi.create(request);
        },
        onSuccess: (organization) => {
            if (isManager && organizationId === organization.id) {
                setOrganizationName(organization.name);
                setDraft(draftFromOrganization(organization));
                queryClient.setQueryData(['organizationEdit', organizationId], organization);
                queryClient.setQueryData(['organizationTheme', organization.id], organization);
            }
            if (selectedOrganizationId === organization.id) {
                setSelectedOrganization(organization.id, organization.name);
                queryClient.setQueryData(['organizationTheme', organization.id], organization);
            }
            if (isAdmin) {
                setDraft(emptyDraft());
                setEditingId(null);
            }
            setError('');
            refresh();
        },
        onError: (value: unknown) => {
            const e = value as { response?: { data?: { message?: string } } };
            setError(e.response?.data?.message ?? 'Unable to save organization.');
        },
    });
    if (!isAdmin && !isManager) return <Navigate to="/" replace />;
    if (isManager && !organizationId) return <Navigate to="/" replace />;

    return <Layout><div className="max-w-4xl mx-auto space-y-5">
        <h1 className="text-2xl font-bold">{isAdmin ? 'Organizations' : 'Organization'}</h1>
        <div className="bg-white rounded-xl shadow p-4 space-y-4">
            <input
                className="border rounded p-2 w-full theme-focus"
                placeholder="Organization name"
                value={draft.name}
                onChange={(event) => setDraft({ ...draft, name: event.target.value })}
            />
            <ThemePicker draft={draft} onChange={setDraft} />
            <div className="flex justify-end gap-2">
                {isAdmin && editingId && (
                    <button className="px-4 py-2 text-slate-600" onClick={() => {
                        setDraft(emptyDraft());
                        setEditingId(null);
                        setError('');
                    }}>Cancel</button>
                )}
                <button
                    className="theme-primary rounded px-4 py-2 disabled:opacity-50"
                    disabled={!draft.name.trim() || mutation.isPending || (isManager && isManagerOrganizationLoading)}
                    onClick={() => mutation.mutate()}
                >
                    {isManager || editingId ? 'Save changes' : 'Create'}
                </button>
            </div>
        </div>
        {error && <p className="text-red-600">{error}</p>}
        {isAdmin && <div className="bg-white rounded-xl shadow overflow-hidden">
            <table className="w-full"><thead><tr className="text-left border-b"><th className="p-3">Name</th><th /></tr></thead>
                <tbody>{data?.items.map((organization) => <tr key={organization.id} className="border-b">
                    <td className="p-3">
                        <span className="font-medium">{organization.name}</span>
                        <span className="ml-2 text-xs text-slate-500 capitalize">
                            {resolveTheme(organization.theme).name}
                        </span>
                    </td>
                    <td className="p-3 text-right space-x-3">
                        <button className="theme-link" onClick={() => {
                            clearOrganizationQueries(queryClient);
                            setSelectedOrganization(organization.id, organization.name);
                            navigate('/retrospectives');
                        }}>Select</button>
                        <button className="theme-link" onClick={() => {
                            setEditingId(organization.id);
                            setDraft(draftFromOrganization(organization));
                            setError('');
                            window.scrollTo({ top: 0, behavior: 'smooth' });
                        }}>Edit</button>
                        <button className="text-red-600" onClick={async () => {
                            if (confirm(`Delete ${organization.name}? All users, retrospectives, assignments, columns, and items in this organization will be permanently deleted.`)) {
                                await organizationsApi.delete(organization.id);
                                if (selectedOrganizationId === organization.id) {
                                    clearOrganizationQueries(queryClient);
                                    clearSelectedOrganization();
                                }
                                refresh();
                            }
                        }}>Delete</button>
                    </td>
                </tr>)}</tbody>
            </table>
        </div>}
        {isAdmin && <div className="flex justify-center gap-4">
            <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
            <span>Page {page}</span>
            <button disabled={!data || page * data.pageSize >= data.totalCount} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>}
    </div></Layout>;
}
