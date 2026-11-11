import { useState } from 'react';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';

import { cancelLeave } from '../api/time-off-api';
import { useTimeOffMutation } from '../hooks/use-time-off-mutation';
import type { LeaveRequest } from '../types';
import { formatDays, formatPeriod, statusLabel, statusTone } from '../utils';

/** A person's requests, newest first, with Cancel where it is still allowed. */
export function RequestList({ requests, showName = false }: { requests: LeaveRequest[]; showName?: boolean }) {
  const [cancelling, setCancelling] = useState<LeaveRequest | null>(null);
  const cancel = useTimeOffMutation({
    mutationFn: (id: string) => cancelLeave(id),
    success: { title: 'Time off cancelled', message: 'The days are back in the balance.' },
    errorTitle: 'Could not cancel',
    onSuccess: () => setCancelling(null)
  });

  return (
    <>
      <ul className="leave-requests">
        {requests.map((request) => (
          <li key={request.id}>
            <span className="leave-requests__swatch" style={{ background: request.color }} aria-hidden="true" />
            <div className="leave-requests__main">
              <strong>
                {showName ? `${request.employeeName} · ` : ''}
                {request.leaveTypeName}, {formatPeriod(request.startDate, request.endDate)}
              </strong>
              <small>
                {formatDays(request.workingDays)}
                {request.reason ? ` · ${request.reason}` : ''}
                {request.status === 'Pending' && request.approverName ? ` · ${request.approverName} decides` : ''}
              </small>
              {request.decisionComment ? <small>“{request.decisionComment}”</small> : null}
            </div>
            <Badge tone={statusTone[request.status]}>{statusLabel[request.status]}</Badge>
            {request.canCancel ? (
              <Button variant="ghost" size="small" onClick={() => setCancelling(request)}>
                Cancel
              </Button>
            ) : null}
          </li>
        ))}
      </ul>
      <ConfirmDialog
        open={cancelling !== null}
        title="Cancel this time off?"
        description={
          cancelling
            ? `${cancelling.leaveTypeName}, ${formatPeriod(cancelling.startDate, cancelling.endDate)}. The ${formatDays(cancelling.workingDays)} go back into the balance.`
            : ''
        }
        confirmLabel="Cancel time off"
        loading={cancel.isPending}
        onConfirm={() => cancelling && cancel.mutate(cancelling.id)}
        onClose={() => setCancelling(null)}
      />
    </>
  );
}
