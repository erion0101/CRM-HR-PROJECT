import { useQuery } from '@tanstack/react-query';
import { CalendarPlus } from 'lucide-react';
import { useState } from 'react';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import { getLeaveTypes, getMyLeave } from '../api/time-off-api';
import { BalanceCards } from '../components/balance-cards';
import { RequestList } from '../components/request-list';
import { RequestTimeOffDialog } from '../components/request-time-off-dialog';
import { TimeOffTabs } from '../components/time-off-tabs';
import { todayIso } from '../utils';

/** The employee's balances and requests, with "Request time off". */
export function MyTimeOffPage() {
  const [requesting, setRequesting] = useState(false);
  const leave = useQuery({ queryKey: ['leave', 'me'], queryFn: () => getMyLeave() });
  const types = useQuery({ queryKey: ['leave', 'types'], queryFn: () => getLeaveTypes() });
  const today = todayIso();
  const upcoming = (leave.data?.requests ?? []).filter(
    (item) => item.endDate >= today && (item.status === 'Pending' || item.status === 'Approved')
  );
  const past = (leave.data?.requests ?? []).filter((item) => !upcoming.includes(item));

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Time off"
        title="My time off"
        description="Your days available this year, and the time off you have asked for."
        actions={
          <Button onClick={() => setRequesting(true)} disabled={!types.data?.length}>
            <CalendarPlus size={16} /> Request time off
          </Button>
        }
      >
        <TimeOffTabs />
      </PageHeader>

      {leave.isLoading ? (
        <Card>
          <Skeleton count={4} />
        </Card>
      ) : leave.isError ? (
        <ErrorState
          title="Your time off could not be loaded"
          description="Your account may not be linked to an employee profile yet. Ask HR if this keeps happening."
          onRetry={() => void leave.refetch()}
        />
      ) : (
        <>
          <BalanceCards balances={leave.data!.balances} />
          <Card>
            <div className="section-heading section-heading--padded">
              <h2>Coming up</h2>
            </div>
            {upcoming.length ? (
              <RequestList requests={upcoming} />
            ) : (
              <div className="state-panel">
                <h3>No time off planned</h3>
                <p>Use Request time off to ask for days. Your manager approves them.</p>
              </div>
            )}
          </Card>
          {past.length ? (
            <Card>
              <div className="section-heading section-heading--padded">
                <h2>Earlier this year</h2>
              </div>
              <RequestList requests={past} />
            </Card>
          ) : null}
        </>
      )}

      {requesting && types.data ? (
        <RequestTimeOffDialog open types={types.data} onClose={() => setRequesting(false)} />
      ) : null}
    </div>
  );
}
