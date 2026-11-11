import { useQuery } from '@tanstack/react-query';
import { Check, X } from 'lucide-react';
import { useState } from 'react';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Textarea } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';

import { decideLeave, getLeaveApprovals } from '../api/time-off-api';
import { TimeOffTabs } from '../components/time-off-tabs';
import { useTimeOffMutation } from '../hooks/use-time-off-mutation';
import type { LeaveApproval } from '../types';
import { formatDays, formatPeriod } from '../utils';

/** Requests waiting for the signed-in manager (their team) or HR (everyone), oldest first. */
export function LeaveApprovalsPage() {
  const approvals = useQuery({ queryKey: ['leave', 'approvals'], queryFn: getLeaveApprovals });
  const [rejecting, setRejecting] = useState<LeaveApproval | null>(null);
  const [comment, setComment] = useState('');
  const decide = useTimeOffMutation({
    mutationFn: ({ id, approve, note }: { id: string; approve: boolean; note?: string }) =>
      decideLeave(id, approve, note),
    errorTitle: 'Could not save the decision',
    onSuccess: () => {
      setRejecting(null);
      setComment('');
    }
  });

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Time off"
        title="Approvals"
        description="Time off your team has asked for. The person is told as soon as you decide."
      >
        <TimeOffTabs />
      </PageHeader>

      {approvals.isLoading ? (
        <Card>
          <Skeleton count={4} />
        </Card>
      ) : approvals.isError ? (
        <ErrorState onRetry={() => void approvals.refetch()} />
      ) : approvals.data!.length ? (
        <Card>
          <ul className="leave-requests">
            {approvals.data!.map((item) => (
              <li key={item.request.id}>
                <span
                  className="leave-requests__swatch"
                  style={{ background: item.request.color }}
                  aria-hidden="true"
                />
                <div className="leave-requests__main">
                  <strong>
                    {item.request.employeeName} · {item.request.leaveTypeName},{' '}
                    {formatPeriod(item.request.startDate, item.request.endDate)}
                  </strong>
                  <small>
                    {formatDays(item.request.workingDays)}
                    {item.availableAfter != null ? ` · ${formatDays(item.availableAfter)} left after this` : ''}
                    {item.request.reason ? ` · “${item.request.reason}”` : ''}
                  </small>
                  <small className={item.teammatesOut.length ? 'leave-requests__warning' : ''}>
                    {item.teammatesOut.length
                      ? `Also off on some of these days: ${item.teammatesOut.join(', ')}`
                      : 'Nobody else in the team is off then.'}
                  </small>
                </div>
                <div className="row-actions">
                  <Button
                    size="small"
                    variant="secondary"
                    onClick={() => {
                      setComment('');
                      setRejecting(item);
                    }}
                  >
                    <X size={15} /> Reject
                  </Button>
                  <Button
                    size="small"
                    loading={decide.isPending && decide.variables?.id === item.request.id && decide.variables.approve}
                    onClick={() => decide.mutate({ id: item.request.id, approve: true })}
                  >
                    <Check size={15} /> Approve
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        </Card>
      ) : (
        <Card>
          <div className="state-panel">
            <h3>Nothing to approve</h3>
            <p>When someone in your team asks for time off, it appears here and you get a notification.</p>
          </div>
        </Card>
      )}

      <Dialog
        open={rejecting !== null}
        onClose={() => setRejecting(null)}
        title="Reject this request?"
        description={
          rejecting
            ? `${rejecting.request.employeeName}, ${rejecting.request.leaveTypeName}, ${formatPeriod(rejecting.request.startDate, rejecting.request.endDate)}.`
            : undefined
        }
        footer={
          <>
            <Button variant="secondary" onClick={() => setRejecting(null)}>
              Keep it waiting
            </Button>
            <Button
              variant="danger"
              disabled={!comment.trim()}
              loading={decide.isPending}
              onClick={() =>
                rejecting && decide.mutate({ id: rejecting.request.id, approve: false, note: comment.trim() })
              }
            >
              Reject
            </Button>
          </>
        }
      >
        <FieldShell label="Reason" htmlFor="rejectReason" required hint="The employee sees this.">
          <Textarea
            id="rejectReason"
            rows={3}
            maxLength={500}
            value={comment}
            onChange={(event) => setComment(event.target.value)}
          />
        </FieldShell>
      </Dialog>
    </div>
  );
}
