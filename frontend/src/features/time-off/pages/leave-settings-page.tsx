import { useQuery } from '@tanstack/react-query';
import { Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';

import {
  archiveLeaveType,
  createPublicHoliday,
  deletePublicHoliday,
  getLeaveTypes,
  getPublicHolidays
} from '../api/time-off-api';
import { LeaveTypeDialog } from '../components/leave-type-dialog';
import { useTimeOffMutation } from '../hooks/use-time-off-mutation';
import type { LeaveType, PublicHoliday } from '../types';
import { formatDay, formatDays, todayIso } from '../utils';

const monthDay = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'long', timeZone: 'UTC' });

function describeRules(type: LeaveType): string {
  if (!type.deductsFromBalance) return 'No allowance; days are only counted';
  const earned =
    type.accrualMethod === 'Monthly'
      ? `${formatDays(type.daysPerYear)} a year, ${formatDays(type.daysPerYear / 12)} a month`
      : `${formatDays(type.daysPerYear)} a year, all on 1 January`;
  if (!type.carriesOver) return `${earned} · unused days do not carry over`;
  const limit = type.maxCarryOverDays != null ? `up to ${formatDays(type.maxCarryOverDays)}` : 'all unused days';
  const expiry =
    type.carryOverExpiryMonth && type.carryOverExpiryDay
      ? `, expire after ${monthDay.format(new Date(Date.UTC(2025, type.carryOverExpiryMonth - 1, type.carryOverExpiryDay)))}`
      : '';
  return `${earned} · ${limit} carry over${expiry}`;
}

/** HR: kinds of leave with their allowance rules, and the public holiday calendar. */
export function LeaveSettingsPage() {
  const types = useQuery({ queryKey: ['leave', 'types', 'all'], queryFn: () => getLeaveTypes(true) });
  const holidays = useQuery({ queryKey: ['leave', 'holidays'], queryFn: getPublicHolidays });
  const [editing, setEditing] = useState<LeaveType | 'new' | null>(null);
  const [archiving, setArchiving] = useState<LeaveType | null>(null);
  const [removing, setRemoving] = useState<PublicHoliday | null>(null);
  const [holidayDate, setHolidayDate] = useState(todayIso());
  const [holidayName, setHolidayName] = useState('');
  const [repeats, setRepeats] = useState(true);

  const archive = useTimeOffMutation({
    mutationFn: (type: LeaveType) => archiveLeaveType(type.id, type.version),
    success: { title: 'Leave type archived', message: 'Nobody can ask for it any more. Past requests keep it.' },
    errorTitle: 'Could not archive',
    onSuccess: () => setArchiving(null)
  });
  const addHoliday = useTimeOffMutation({
    mutationFn: createPublicHoliday,
    success: { title: 'Holiday added' },
    errorTitle: 'Could not add the holiday',
    onSuccess: () => setHolidayName('')
  });
  const removeHoliday = useTimeOffMutation({
    mutationFn: (holiday: PublicHoliday) => deletePublicHoliday(holiday.id),
    success: { title: 'Holiday removed', message: 'Requests already sent keep the days they were counted with.' },
    errorTitle: 'Could not remove the holiday',
    onSuccess: () => setRemoving(null)
  });

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Settings"
        title="Time off and holidays"
        description="The kinds of leave people can ask for, how many days they get, and the public holidays nobody works."
        actions={
          <Button onClick={() => setEditing('new')}>
            <Plus size={16} /> Add a leave type
          </Button>
        }
      />

      <Card>
        <div className="section-heading section-heading--padded">
          <h2>Leave types</h2>
        </div>
        {types.isLoading ? (
          <Skeleton count={3} />
        ) : types.isError ? (
          <ErrorState onRetry={() => void types.refetch()} />
        ) : (
          <ul className="leave-requests">
            {types.data!.map((type) => (
              <li key={type.id}>
                <span className="leave-requests__swatch" style={{ background: type.color }} aria-hidden="true" />
                <div className="leave-requests__main">
                  <strong>{type.name}</strong>
                  <small>{describeRules(type)}</small>
                  <small>
                    {type.isPaid ? 'Paid' : 'Unpaid'} ·{' '}
                    {type.requiresApproval ? 'Manager approves' : 'Approved straight away'}
                  </small>
                </div>
                {type.isArchived ? (
                  <Badge>Archived</Badge>
                ) : (
                  <div className="row-actions">
                    <Button size="small" variant="ghost" onClick={() => setArchiving(type)}>
                      Archive
                    </Button>
                    <Button size="small" variant="secondary" onClick={() => setEditing(type)}>
                      Change
                    </Button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </Card>

      <Card>
        <div className="section-heading section-heading--padded">
          <h2>Public holidays</h2>
          <p>These days are not counted when someone takes time off.</p>
        </div>
        <form
          className="leave-holiday-form"
          onSubmit={(event) => {
            event.preventDefault();
            if (holidayName.trim())
              addHoliday.mutate({ date: holidayDate, name: holidayName.trim(), repeatsYearly: repeats });
          }}
        >
          <FieldShell label="Date" htmlFor="holidayDate" required>
            <Input
              id="holidayDate"
              type="date"
              value={holidayDate}
              onChange={(event) => setHolidayDate(event.target.value)}
            />
          </FieldShell>
          <FieldShell label="Name" htmlFor="holidayName" required>
            <Input
              id="holidayName"
              maxLength={120}
              value={holidayName}
              onChange={(event) => setHolidayName(event.target.value)}
            />
          </FieldShell>
          <label className="checkbox-row">
            <input type="checkbox" checked={repeats} onChange={(event) => setRepeats(event.target.checked)} /> Every
            year
          </label>
          <Button type="submit" variant="secondary" loading={addHoliday.isPending} disabled={!holidayName.trim()}>
            Add holiday
          </Button>
        </form>
        {holidays.isLoading ? (
          <Skeleton count={3} />
        ) : holidays.data?.length ? (
          <ul className="leave-requests">
            {holidays.data.map((holiday) => (
              <li key={holiday.id}>
                <div className="leave-requests__main">
                  <strong>{holiday.name}</strong>
                  <small>
                    {holiday.repeatsYearly
                      ? `Every year on ${monthDay.format(new Date(`${holiday.date}T00:00:00Z`))}, from ${holiday.date.slice(0, 4)}`
                      : formatDay(holiday.date)}
                  </small>
                </div>
                <Button
                  size="small"
                  variant="ghost"
                  aria-label={`Remove ${holiday.name}`}
                  onClick={() => setRemoving(holiday)}
                >
                  <Trash2 size={15} />
                </Button>
              </li>
            ))}
          </ul>
        ) : (
          <div className="state-panel">
            <h3>No public holidays yet</h3>
            <p>Add them above. Holidays that move each year (such as Easter) are added once per year.</p>
          </div>
        )}
      </Card>

      {editing ? (
        <LeaveTypeDialog type={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} />
      ) : null}
      <ConfirmDialog
        open={archiving !== null}
        title={`Archive ${archiving?.name ?? ''}?`}
        description="Nobody can ask for this kind of leave any more. Requests already made keep it."
        confirmLabel="Archive"
        loading={archive.isPending}
        onConfirm={() => archiving && archive.mutate(archiving)}
        onClose={() => setArchiving(null)}
      />
      <ConfirmDialog
        open={removing !== null}
        title={`Remove ${removing?.name ?? ''}?`}
        description="New requests will count this day as a working day. Requests already sent do not change."
        confirmLabel="Remove"
        loading={removeHoliday.isPending}
        onConfirm={() => removing && removeHoliday.mutate(removing)}
        onClose={() => setRemoving(null)}
      />
    </div>
  );
}
