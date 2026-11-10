import { ChevronDown, ChevronUp, Minus, Plus, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router';

import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { SearchInput } from '@/components/forms/search-input';
import { PageHeader } from '@/components/navigation/page-header';
import { Avatar } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useEmployees } from '@/features/employees/hooks/use-employees';
import type { Employee } from '@/features/employees/types';

import { PeopleWorkspaceTabs } from '../components/people-workspace-tabs';
import { usePositions } from '../hooks/use-organization';

export function OrganizationChartPage() {
  const employees = useEmployees({ limit: 100 });
  const positions = usePositions(undefined, 100);
  const [params] = useSearchParams();
  const [search, setSearch] = useState('');
  const [zoom, setZoom] = useState(1);
  const selectedId = params.get('employee');
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const people = useMemo(() => employees.data?.items ?? [], [employees.data?.items]);
  const roots = useMemo(() => {
    const ids = new Set(people.map((employee) => employee.id));
    const result = people.filter((employee) => !employee.managerId || !ids.has(employee.managerId));
    return result.length ? result : people;
  }, [people]);
  const matchingIds = useMemo(() => {
    const term = search.trim().toLowerCase();
    return new Set(
      term
        ? people
            .filter((employee) => `${employee.firstName} ${employee.lastName}`.toLowerCase().includes(term))
            .map((employee) => employee.id)
        : []
    );
  }, [people, search]);

  const toggle = (id: string) =>
    setCollapsed((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <div className="page-stack org-chart-page">
      <PageHeader
        eyebrow="People"
        title="Organization chart"
        description="Explore the effective-dated reporting structure available within your access scope."
      >
        <PeopleWorkspaceTabs />
        <div className="org-chart-toolbar">
          <SearchInput
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Find an employee"
            aria-label="Find employee in organization chart"
          />
          <div className="zoom-control">
            <button onClick={() => setZoom((value) => Math.max(0.65, value - 0.1))} aria-label="Zoom out">
              <Minus size={16} />
            </button>
            <span>{Math.round(zoom * 100)}%</span>
            <button onClick={() => setZoom((value) => Math.min(1.35, value + 0.1))} aria-label="Zoom in">
              <Plus size={16} />
            </button>
          </div>
        </div>
      </PageHeader>
      <Card className="org-chart-canvas">
        {employees.isLoading || positions.isLoading ? (
          <div className="skeleton-table">
            <Skeleton count={6} />
          </div>
        ) : employees.isError ? (
          <ErrorState
            title="Organization chart could not be loaded"
            description="Employee reporting data is temporarily unavailable."
            onRetry={() => void employees.refetch()}
          />
        ) : roots.length ? (
          <div className="org-chart-scroll">
            <div className="org-chart-tree" style={{ transform: `scale(${zoom})` }}>
              {roots.map((root) => (
                <OrgBranch
                  key={root.id}
                  employee={root}
                  employees={people}
                  positions={positions.data?.items ?? []}
                  collapsed={collapsed}
                  matchingIds={matchingIds}
                  selectedId={selectedId}
                  toggle={toggle}
                  visited={new Set()}
                />
              ))}
            </div>
          </div>
        ) : (
          <EmptyState
            title="Organization structure has not been configured"
            description="Add effective-dated manager assignments to build the reporting chart."
          />
        )}
        {employees.data?.nextCursor ? (
          <div className="org-chart-notice">
            <Search size={15} /> The chart shows the first 100 accessible employees. Narrow access scopes remain
            enforced.
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function OrgBranch({
  employee,
  employees,
  positions,
  collapsed,
  matchingIds,
  selectedId,
  toggle,
  visited
}: {
  employee: Employee;
  employees: Employee[];
  positions: { id: string; title: string }[];
  collapsed: Set<string>;
  matchingIds: Set<string>;
  selectedId: string | null;
  toggle: (id: string) => void;
  visited: Set<string>;
}) {
  if (visited.has(employee.id)) return null;
  const nextVisited = new Set(visited).add(employee.id);
  const reports = employees.filter((candidate) => candidate.managerId === employee.id);
  const isCollapsed = collapsed.has(employee.id);
  const highlighted = selectedId === employee.id || matchingIds.has(employee.id);
  return (
    <div className="org-branch">
      <article className={`org-node ${highlighted ? 'is-highlighted' : ''}`}>
        <Link to={`/employees/${employee.id}`}>
          <Avatar name={`${employee.firstName} ${employee.lastName}`} size="medium" />
          <span>
            <strong>
              {employee.firstName} {employee.lastName}
            </strong>
            <small>{positions.find((item) => item.id === employee.positionId)?.title ?? 'Position not assigned'}</small>
          </span>
        </Link>
        {reports.length ? (
          <Button
            variant="ghost"
            size="small"
            onClick={() => toggle(employee.id)}
            aria-label={`${isCollapsed ? 'Expand' : 'Collapse'} ${employee.firstName}'s team`}
          >
            <span>{reports.length}</span>
            {isCollapsed ? <ChevronDown size={14} /> : <ChevronUp size={14} />}
          </Button>
        ) : null}
      </article>
      {reports.length && !isCollapsed ? (
        <div className="org-children">
          {reports.map((report) => (
            <OrgBranch
              key={report.id}
              employee={report}
              employees={employees}
              positions={positions}
              collapsed={collapsed}
              matchingIds={matchingIds}
              selectedId={selectedId}
              toggle={toggle}
              visited={nextVisited}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}
