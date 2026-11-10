import { Search } from 'lucide-react';
import { useState } from 'react';

import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { FieldShell, Input } from '@/components/forms/field';
import { CursorPagination } from '@/components/navigation/cursor-pagination';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';

import { useAuditEvents } from '../hooks/use-administration';

export function AuditPage() {
  const [entityType, setEntityType] = useState('');
  const [from, setFrom] = useState('');
  const [cursor, setCursor] = useState<string | undefined>();
  const [history, setHistory] = useState<(string | undefined)[]>([]);
  const query = useAuditEvents({
    entityType: entityType || undefined,
    from: from ? new Date(from).toISOString() : undefined,
    cursor,
    limit: 30
  });
  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Administration"
        title="Audit history"
        description="Review security-sensitive and business record activity captured by the backend."
      >
        <div className="filter-bar">
          <label className="search-control">
            <Search size={17} />
            <input
              value={entityType}
              onChange={(event) => {
                setEntityType(event.target.value);
                setCursor(undefined);
              }}
              placeholder="Filter by entity type"
              aria-label="Entity type"
            />
          </label>
          <FieldShell label="From date" htmlFor="auditFrom">
            <Input
              id="auditFrom"
              type="date"
              value={from}
              onChange={(event) => {
                setFrom(event.target.value);
                setCursor(undefined);
              }}
            />
          </FieldShell>
        </div>
      </PageHeader>
      <Card className="table-card">
        {query.isError ? (
          <ErrorState title="Audit history could not be loaded" onRetry={() => void query.refetch()} />
        ) : query.data?.items.length ? (
          <>
            <div className="data-table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Actor</th>
                    <th>Action</th>
                    <th>Entity</th>
                    <th>Record</th>
                    <th>Correlation</th>
                  </tr>
                </thead>
                <tbody>
                  {query.data.items.map((event) => (
                    <tr key={event.id}>
                      <td>
                        {new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(
                          new Date(event.occurredAtUtc)
                        )}
                      </td>
                      <td>{event.actorEmail ?? 'System'}</td>
                      <td>
                        <Badge tone={event.action.toLowerCase().includes('denied') ? 'negative' : 'info'}>
                          {event.action}
                        </Badge>
                      </td>
                      <td>{event.entityType}</td>
                      <td className="mono">{event.entityId.slice(0, 12)}</td>
                      <td className="mono muted">{event.correlationId?.slice(0, 12) ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
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
          </>
        ) : (
          <EmptyState
            title="No audit events match"
            description="Events will appear here as users work with secured records."
          />
        )}
      </Card>
    </div>
  );
}
