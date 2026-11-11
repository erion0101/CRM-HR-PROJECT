import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import { canManageLeave } from '@/app/access';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/hooks/use-auth';

import { adjustLeaveBalance, getEmployeeLeave } from '../api/time-off-api';
import { useTimeOffMutation } from '../hooks/use-time-off-mutation';
import { formatDays } from '../utils';
import { BalanceCards } from './balance-cards';
import { RequestList } from './request-list';

/** The Time off tab on an employee profile: balances, requests, and (for HR) balance adjustments. */
export function EmployeeTimeOffTab({ employeeId }: { employeeId: string }) {
  const { user } = useAuth();
  const isHr = canManageLeave(user);
  const thisYear = new Date().getFullYear();
  const [year, setYear] = useState(thisYear);
  const [adjusting, setAdjusting] = useState(false);
  const leave = useQuery({
    queryKey: ['leave', 'employee', employeeId, year],
    queryFn: () => getEmployeeLeave(employeeId, year)
  });

  if (leave.isLoading)
    return (
      <Card>
        <Skeleton count={4} />
      </Card>
    );
  if (leave.isError) return <ErrorState onRetry={() => void leave.refetch()} />;
  const data = leave.data!;
  const withBalance = data.balances.filter((balance) => balance.deductsFromBalance);

  return (
    <div className="page-stack">
      <div className="progress-toolbar">
        <div className="segmented" role="group" aria-label="Year">
          {[thisYear - 1, thisYear, thisYear + 1].map((item) => (
            <button
              type="button"
              key={item}
              aria-pressed={year === item}
              className={year === item ? 'is-active' : ''}
              onClick={() => setYear(item)}
            >
              {item}
            </button>
          ))}
        </div>
        {isHr && withBalance.length ? (
          <Button size="small" variant="secondary" onClick={() => setAdjusting(true)}>
            Adjust balance
          </Button>
        ) : null}
      </div>
      <BalanceCards balances={data.balances} />
      <Card>
        <div className="section-heading section-heading--padded">
          <h2>Requests</h2>
        </div>
        {data.requests.length ? (
          <RequestList requests={data.requests} />
        ) : (
          <div className="state-panel">
            <h3>No time off in {year}</h3>
            <p>Requests appear here when {data.employeeName} asks for time off.</p>
          </div>
        )}
      </Card>
      {data.adjustments.length ? (
        <Card>
          <div className="section-heading section-heading--padded">
            <h2>Adjustments by HR</h2>
          </div>
          <ul className="leave-requests">
            {data.adjustments.map((item) => (
              <li key={item.id}>
                <div className="leave-requests__main">
                  <strong>
                    {item.days > 0 ? '+' : ''}
                    {formatDays(item.days)} {item.leaveTypeName}
                  </strong>
                  <small>{item.reason}</small>
                </div>
              </li>
            ))}
          </ul>
        </Card>
      ) : null}
      {adjusting ? (
        <AdjustDialog
          employeeId={employeeId}
          employeeName={data.employeeName}
          year={year}
          types={withBalance.map((balance) => ({ id: balance.leaveTypeId, name: balance.leaveTypeName }))}
          onClose={() => setAdjusting(false)}
        />
      ) : null}
    </div>
  );
}

function AdjustDialog({
  employeeId,
  employeeName,
  year,
  types,
  onClose
}: {
  employeeId: string;
  employeeName: string;
  year: number;
  types: { id: string; name: string }[];
  onClose: () => void;
}) {
  const [leaveTypeId, setLeaveTypeId] = useState(types[0]?.id ?? '');
  const [days, setDays] = useState('1');
  const [reason, setReason] = useState('');
  const save = useTimeOffMutation({
    mutationFn: () => adjustLeaveBalance(employeeId, { leaveTypeId, year, days: Number(days), reason: reason.trim() }),
    success: { title: 'Balance adjusted' },
    errorTitle: 'Could not adjust the balance',
    onSuccess: onClose
  });
  const valid = leaveTypeId && Number(days) !== 0 && !Number.isNaN(Number(days)) && reason.trim();
  return (
    <Dialog
      open
      onClose={onClose}
      title={`Adjust ${employeeName}'s ${year} balance`}
      description="Add days with a positive number, remove them with a negative one. The reason is kept with the change."
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Close
          </Button>
          <Button loading={save.isPending} disabled={!valid} onClick={() => save.mutate(undefined)}>
            Save
          </Button>
        </>
      }
    >
      <div className="form-stack">
        <div className="form-grid">
          <FieldShell label="Kind of leave" htmlFor="adjustType" required>
            <Select id="adjustType" value={leaveTypeId} onChange={(event) => setLeaveTypeId(event.target.value)}>
              {types.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.name}
                </option>
              ))}
            </Select>
          </FieldShell>
          <FieldShell label="Days" htmlFor="adjustDays" required>
            <Input
              id="adjustDays"
              type="number"
              step={0.5}
              value={days}
              onChange={(event) => setDays(event.target.value)}
            />
          </FieldShell>
        </div>
        <FieldShell label="Reason" htmlFor="adjustReason" required>
          <Input id="adjustReason" maxLength={500} value={reason} onChange={(event) => setReason(event.target.value)} />
        </FieldShell>
      </div>
    </Dialog>
  );
}
