import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/Layout';
import { organizationsApi } from '../api/organizations';
import { clearOrganizationQueries } from '../query/organizationQueries';
import { useAuthStore } from '../store/authStore';

export function OrganizationSelectorPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const role = useAuthStore((state) => state.role);
    const setSelectedOrganization = useAuthStore((state) => state.setSelectedOrganization);
    const [page, setPage] = useState(1);
    const { data, isLoading, isError } = useQuery({
        queryKey: ['organizations', 'selector', page],
        queryFn: () => organizationsApi.getAll(page),
        enabled: role === 'Admin',
    });

    if (role !== 'Admin') return <Navigate to="/retrospectives" replace />;

    function selectOrganization(id: string, name: string) {
        clearOrganizationQueries(queryClient);
        setSelectedOrganization(id, name);
        navigate('/retrospectives');
    }

    return (
        <Layout>
            <div className="max-w-4xl mx-auto space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-slate-800">Select an organization</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Choose the organization you want to work with.
                    </p>
                </div>

                {isLoading && <p className="text-slate-500">Loading organizations…</p>}
                {isError && <p className="text-red-600">Failed to load organizations.</p>}
                {!isLoading && !isError && data?.items.length === 0 && (
                    <div className="bg-white rounded-xl border border-slate-200 shadow p-6 text-center">
                        <p className="text-slate-600">No organizations have been created yet.</p>
                        <button
                            onClick={() => navigate('/organizations')}
                            className="mt-4 theme-primary text-white rounded-md px-4 py-2"
                        >
                            Manage organizations
                        </button>
                    </div>
                )}

                <div className="grid sm:grid-cols-2 gap-4">
                    {data?.items.map((organization) => (
                        <button
                            key={organization.id}
                            onClick={() => selectOrganization(organization.id, organization.name)}
                            className="bg-white rounded-xl border border-slate-200 shadow-sm p-5 text-left hover:shadow transition theme-focus"
                        >
                            <span className="block font-semibold text-slate-800">{organization.name}</span>
                            <span className="block mt-1 text-sm theme-link">Select organization →</span>
                        </button>
                    ))}
                </div>

                {data && data.totalCount > data.pageSize && (
                    <div className="flex justify-center gap-4">
                        <button disabled={page === 1} onClick={() => setPage((value) => value - 1)}>
                            Previous
                        </button>
                        <span>Page {page}</span>
                        <button
                            disabled={page * data.pageSize >= data.totalCount}
                            onClick={() => setPage((value) => value + 1)}
                        >
                            Next
                        </button>
                    </div>
                )}
            </div>
        </Layout>
    );
}
