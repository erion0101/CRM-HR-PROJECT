import { Building2, Mail, Network, Phone, Plus, UserPlus, UserRound } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router';

import { canManagePeople } from '@/app/access';
import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Select } from '@/components/forms/field';
import { SearchInput } from '@/components/forms/search-input';
import { PageHeader } from '@/components/navigation/page-header';
import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { PeopleWorkspaceTabs } from '@/features/organization/components/people-workspace-tabs';
import { useDepartments, usePositions } from '@/features/organization/hooks/use-organization';
import { useDebouncedValue } from '@/hooks/use-debounced-value';

import { useEmployees } from '../hooks/use-employees';
import type { Employee, EmploymentStatus } from '../types';

type GroupMode = 'name' | 'department' | 'status';

export function EmployeesPage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const [search, setSearch] = useState(params.get('search') ?? '');
  const [status, setStatus] = useState<EmploymentStatus | ''>('');
  const [departmentId, setDepartmentId] = useState(params.get('departmentId') ?? '');
  const [groupBy, setGroupBy] = useState<GroupMode>('name');
  const debouncedSearch = useDebouncedValue(search, 250);
  const query = useEmployees({
    search: debouncedSearch || undefined,
    status,
    departmentId: departmentId || undefined,
    limit: 100
  });
  const departments = useDepartments(undefined, 100);
  const positions = usePositions(undefined, 100);

  const groups = useMemo(() => {
    const employees = query.data?.items ?? [];
    const result = new Map<string, Employee[]>();
    for (const employee of employees) {
      const key =
        groupBy === 'name'
          ? employee.lastName.slice(0, 1).toUpperCase() || '#'
          : groupBy === 'department'
            ? (departments.data?.items.find((item) => item.id === employee.departmentId)?.name ?? 'Not assigned')
            : (employee.status ?? 'Not configured');
      result.set(key, [...(result.get(key) ?? []), employee]);
    }
    return [...result.entries()].sort(([left], [right]) => left.localeCompare(right));
  }, [departments.data?.items, groupBy, query.data?.items]);

  return (
    <div className="page-stack directory-page">
      <PageHeader
        eyebrow="People"
        title="Employee directory"
        description="Browse colleagues, reporting relationships, roles, and organization assignments."
        actions={
          canManagePeople(user) ? (
            <div className="row-actions">
              <Link className="button button--secondary button--medium" to="/employees/new">
                <Plus size={17} /> Add employee only
              </Link>
              <Link className="button button--primary button--medium" to="/employees/new-hire">
                <UserPlus size={17} /> New hire
              </Link>
            </div>
          ) : undefined
        }
      >
        <PeopleWorkspaceTabs />
        <div className="directory-toolbar">
          <SearchInput
            value={search}
            onChange={(event) => {
              const value = event.target.value;
              setSearch(value);
              const next = new URLSearchParams(params);
              if (value) next.set('search', value);
              else next.delete('search');
              setParams(next, { replace: true });
            }}
            placeholder="Search directory"
            aria-label="Search directory"
          />
          <label>
            <span>Group by</span>
            <Select value={groupBy} onChange={(event) => setGroupBy(event.target.value as GroupMode)}>
              <option value="name">Name</option>
              <option value="department">Department</option>
              <option value="status">Employment status</option>
            </Select>
          </label>
          <label>
            <span>Department</span>
            <Select
              value={departmentId}
              onChange={(event) => {
                const value = event.target.value;
                setDepartmentId(value);
                const next = new URLSearchParams(params);
                if (value) next.set('departmentId', value);
                else next.delete('departmentId');
                setParams(next, { replace: true });
              }}
            >
              <option value="">All departments</option>
              {departments.data?.items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </Select>
          </label>
          <label>
            <span>Status</span>
            <Select value={status} onChange={(event) => setStatus(event.target.value as EmploymentStatus | '')}>
              <option value="">All statuses</option>
              <option>Active</option>
              <option>OnLeave</option>
              <option>Suspended</option>
              <option>Terminated</option>
            </Select>
          </label>
        </div>
      </PageHeader>

      <div className="directory-summary">
        <strong>{query.data?.items.length ?? 0}</strong>
        <span>employees in your access scope</span>
        {query.data?.nextCursor ? <small>Showing the first 100 matching employees</small> : null}
      </div>

      {query.isLoading ? (
        <Card className="skeleton-table">
          <Skeleton count={8} />
        </Card>
      ) : query.isError ? (
        <Card>
          <ErrorState
            title="Directory could not be loaded"
            description="The employee service is temporarily unavailable."
            onRetry={() => void query.refetch()}
          />
        </Card>
      ) : groups.length ? (
        <div className="directory-groups">
          {groups.map(([group, employees]) => (
            <section key={group} className="directory-group">
              <h2>
                {group}
                <span>{employees.length}</span>
              </h2>
              <Card>
                {employees.map((employee) => (
                  <DirectoryEmployee
                    key={employee.id}
                    employee={employee}
                    employees={query.data?.items ?? []}
                    departmentName={departments.data?.items.find((item) => item.id === employee.departmentId)?.name}
                    positionName={positions.data?.items.find((item) => item.id === employee.positionId)?.title}
                  />
                ))}
              </Card>
            </section>
          ))}
        </div>
      ) : (
        <Card>
          <EmptyState
            title="No employees found"
            description="Try changing the directory search or filters."
            actionLabel={canManagePeople(user) ? 'Add employee' : undefined}
            onAction={
              canManagePeople(user)
                ? () => {
                    window.location.href = '/employees/new';
                  }
                : undefined
            }
          />
        </Card>
      )}
    </div>
  );
}

function DirectoryEmployee({
  employee,
  employees,
  departmentName,
  positionName
}: {
  employee: Employee;
  employees: Employee[];
  departmentName?: string;
  positionName?: string;
}) {
  const manager = employees.find((item) => item.id === employee.managerId);
  return (
    <article className="directory-person">
      <Link className="directory-person__identity" to={`/employees/${employee.id}`}>
        <Avatar name={`${employee.firstName} ${employee.lastName}`} size="large" />
        <span>
          <strong>
            {employee.firstName} {employee.lastName}
          </strong>
          <small>{positionName ?? 'Position not assigned'}</small>
          <small>
            <Building2 size={13} /> {departmentName ?? 'Department not assigned'}
          </small>
        </span>
      </Link>
      <div className="directory-person__contact">
        <span>
          <Mail size={15} /> {employee.workEmail}
        </span>
        <span>
          <Phone size={15} /> {employee.phone ?? 'Phone not provided'}
        </span>
      </div>
      <div className="directory-person__reporting">
        <span>
          <UserRound size={15} />{' '}
          {manager ? (
            <>
              Reports to{' '}
              <Link to={`/employees/${manager.id}`}>
                {manager.firstName} {manager.lastName}
              </Link>
            </>
          ) : (
            'Manager not assigned'
          )}
        </span>
        <Link to={`/organization-chart?employee=${employee.id}`}>
          <Network size={15} /> View in org chart
        </Link>
      </div>
      <Badge
        tone={
          employee.status === 'Active'
            ? 'positive'
            : employee.status === 'Terminated'
              ? 'negative'
              : employee.status
                ? 'warning'
                : 'neutral'
        }
      >
        {employee.status ?? 'Not set'}
      </Badge>
    </article>
  );
}
