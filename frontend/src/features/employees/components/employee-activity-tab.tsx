import { Activity, FileText, UserRound } from 'lucide-react';
import { useState } from 'react';

import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { CursorPagination } from '@/components/navigation/cursor-pagination';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';

import { useEmployeeActivity } from '../hooks/use-employees';

const entityLabels: Record<string, string> = {
  Employee: 'Employee profile',
  Employment: 'Employment status',
  PositionAssignment: 'Position assignment',
  ReportingLine: 'Manager assignment',
  Contract: 'Contract',
  ContractVersion: 'Contract revision',
  CompensationRecord: 'Compensation',
  EmployeeDocument: 'Document',
  DocumentVersion: 'Document version',
  DocumentAssociation: 'Document link'
};

export function EmployeeActivityTab({ employeeId }: { employeeId: string }) {
  const [cursor, setCursor] = useState<string>();
  const [history, setHistory] = useState<(string | undefined)[]>([]);
  const query = useEmployeeActivity(employeeId, cursor);

  if (query.isLoading)
    return (
      <Card>
        <Skeleton count={5} />
      </Card>
    );
  if (query.isError)
    return (
      <Card>
        <ErrorState title="Employee activity could not be loaded" onRetry={() => void query.refetch()} />
      </Card>
    );
  if (!query.data?.items.length)
    return (
      <Card>
        <EmptyState
          title="No employee activity yet"
          description="Profile, employment, contract, and document changes will appear here."
        />
      </Card>
    );

  return (
    <Card className="activity-card">
      <header className="section-heading section-heading--padded">
        <div>
          <h2>Employee activity</h2>
          <p>Server-recorded changes related to this employee.</p>
        </div>
      </header>
      <div className="activity-list">
        {query.data.items.map((event) => (
          <article key={event.id} className="activity-row">
            <span className="activity-row__icon">
              {event.entityType.includes('Document') ? (
                <FileText size={17} />
              ) : event.entityType === 'Employee' ? (
                <UserRound size={17} />
              ) : (
                <Activity size={17} />
              )}
            </span>
            <div>
              <div className="activity-row__title">
                <strong>{entityLabels[event.entityType] ?? event.entityType}</strong>
                <Badge tone={event.action === 'Deleted' ? 'negative' : 'info'}>{event.action}</Badge>
              </div>
              <p>{event.actorEmail ? `Changed by ${event.actorEmail}` : 'System activity'}</p>
              <small>
                {new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(
                  new Date(event.occurredAtUtc)
                )}
              </small>
            </div>
          </article>
        ))}
      </div>
      <CursorPagination
        page={history.length + 1}
        canPrevious={history.length > 0}
        canNext={Boolean(query.data.nextCursor)}
        onPrevious={() => {
          const next = [...history];
          setCursor(next.pop());
          setHistory(next);
        }}
        onNext={() => {
          setHistory((items) => [...items, cursor]);
          setCursor(query.data?.nextCursor ?? undefined);
        }}
      />
    </Card>
  );
}
