import { BriefcaseBusiness, CalendarRange, UserRound } from 'lucide-react';

import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';

import { useAssignmentHistory, useEmploymentHistory, useReportingLineHistory } from '../hooks/use-employees';

const date = (value: string | null) =>
  value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(`${value}T00:00:00`)) : 'Present';

export function EmploymentHistoryTab({ employeeId }: { employeeId: string }) {
  const employment = useEmploymentHistory(employeeId);
  const assignments = useAssignmentHistory(employeeId);
  const reporting = useReportingLineHistory(employeeId);
  const loading = employment.isLoading || assignments.isLoading || reporting.isLoading;
  const failed = employment.isError || assignments.isError || reporting.isError;

  if (loading)
    return (
      <div className="history-grid">
        <Skeleton count={3} />
      </div>
    );
  if (failed)
    return (
      <Card>
        <ErrorState
          title="Employment history could not be loaded"
          onRetry={() => {
            void employment.refetch();
            void assignments.refetch();
            void reporting.refetch();
          }}
        />
      </Card>
    );

  return (
    <div className="history-grid">
      <HistoryCard title="Employment status" icon={CalendarRange} empty="No employment history recorded.">
        {employment.data?.map((item) => (
          <HistoryItem
            key={item.id}
            title={item.status}
            period={`${date(item.startDate)} — ${date(item.endDate)}`}
            badge={item.status}
          />
        ))}
      </HistoryCard>
      <HistoryCard title="Position assignments" icon={BriefcaseBusiness} empty="No position assignments recorded.">
        {assignments.data?.map((item) => (
          <HistoryItem
            key={item.id}
            title={item.position.name}
            detail={item.department.name}
            period={`${date(item.effectiveFrom)} — ${date(item.effectiveTo)}`}
          />
        ))}
      </HistoryCard>
      <HistoryCard title="Reporting lines" icon={UserRound} empty="No manager history recorded.">
        {reporting.data?.map((item) => (
          <HistoryItem
            key={item.id}
            title={item.manager.fullName}
            detail={item.manager.workEmail}
            period={`${date(item.effectiveFrom)} — ${date(item.effectiveTo)}`}
          />
        ))}
      </HistoryCard>
    </div>
  );
}

function HistoryCard({
  title,
  icon: Icon,
  empty,
  children
}: {
  title: string;
  icon: typeof CalendarRange;
  empty: string;
  children: React.ReactNode;
}) {
  const hasItems = Array.isArray(children) ? children.length > 0 : Boolean(children);
  return (
    <Card className="history-card">
      <header>
        <span className="detail-icon">
          <Icon size={18} />
        </span>
        <h2>{title}</h2>
      </header>
      {hasItems ? (
        <div className="timeline">{children}</div>
      ) : (
        <EmptyState title={empty} description="New effective-dated changes will appear here." />
      )}
    </Card>
  );
}

function HistoryItem({
  title,
  detail,
  period,
  badge
}: {
  title: string;
  detail?: string;
  period: string;
  badge?: string;
}) {
  return (
    <div className="timeline__item">
      <i />
      <div>
        <div className="timeline__title">
          <strong>{title}</strong>
          {badge ? <Badge tone={badge === 'Active' ? 'positive' : 'neutral'}>{badge}</Badge> : null}
        </div>
        {detail ? <span>{detail}</span> : null}
        <small>{period}</small>
      </div>
    </div>
  );
}
