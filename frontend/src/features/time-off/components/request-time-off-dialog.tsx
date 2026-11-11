import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/ui/dialog';
import { useDebouncedValue } from '@/hooks/use-debounced-value';

import { previewLeave, requestLeave } from '../api/time-off-api';
import { useTimeOffMutation } from '../hooks/use-time-off-mutation';
import type { LeaveType } from '../types';
import { formatDay, formatDays, todayIso } from '../utils';

/** Pick the kind of leave and the days; the working days and the balance after the request update as you type. */
export function RequestTimeOffDialog({
  open,
  types,
  onClose
}: {
  open: boolean;
  types: LeaveType[];
  onClose: () => void;
}) {
  const [leaveTypeId, setLeaveTypeId] = useState(types[0]?.id ?? '');
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState(todayIso());
  const [reason, setReason] = useState('');
  const selectedType = leaveTypeId || types[0]?.id || '';
  const range = useDebouncedValue({ selectedType, startDate, endDate }, 250);
  const preview = useQuery({
    queryKey: ['leave', 'preview', range.selectedType, range.startDate, range.endDate],
    queryFn: () => previewLeave(range.selectedType, range.startDate, range.endDate),
    enabled: open && Boolean(range.selectedType && range.startDate && range.endDate)
  });
  const submit = useTimeOffMutation({
    mutationFn: requestLeave,
    success: {
      title: 'Request sent',
      message: 'Your manager has been told. You will get a notification when they decide.'
    },
    errorTitle: 'Could not send the request',
    onSuccess: onClose
  });
  const result = preview.data;
  const stale = range.startDate !== startDate || range.endDate !== endDate || range.selectedType !== selectedType;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Request time off"
      description="Whole days only. Weekends and public holidays are not counted."
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Close
          </Button>
          <Button
            loading={submit.isPending}
            disabled={!result || Boolean(result.problem) || stale || preview.isFetching}
            onClick={() =>
              submit.mutate({ leaveTypeId: selectedType, startDate, endDate, reason: reason.trim() || undefined })
            }
          >
            Send request
          </Button>
        </>
      }
    >
      <div className="form-stack">
        <FieldShell label="Kind of leave" htmlFor="leaveType" required>
          <Select id="leaveType" value={selectedType} onChange={(event) => setLeaveTypeId(event.target.value)}>
            {types.map((type) => (
              <option value={type.id} key={type.id}>
                {type.name}
              </option>
            ))}
          </Select>
        </FieldShell>
        <div className="form-grid">
          <FieldShell label="First day off" htmlFor="leaveStart" required>
            <Input
              id="leaveStart"
              type="date"
              value={startDate}
              onChange={(event) => {
                setStartDate(event.target.value);
                if (event.target.value > endDate) setEndDate(event.target.value);
              }}
            />
          </FieldShell>
          <FieldShell label="Last day off" htmlFor="leaveEnd" required>
            <Input
              id="leaveEnd"
              type="date"
              min={startDate}
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
            />
          </FieldShell>
        </div>
        <FieldShell label="Note for your manager" htmlFor="leaveReason" hint="Optional">
          <Textarea
            id="leaveReason"
            rows={2}
            maxLength={500}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </FieldShell>

        <div className={`leave-preview ${result?.problem ? 'leave-preview--problem' : ''}`} aria-live="polite">
          {!result ? (
            <p>Working out the days…</p>
          ) : (
            <>
              <p>
                <strong>{formatDays(result.workingDays)}</strong> off
                {result.availableAfter != null
                  ? ` · ${formatDays(result.availableBefore ?? 0)} available, ${formatDays(result.availableAfter)} left after`
                  : ''}
              </p>
              {result.holidays.length ? (
                <p>
                  Not counted:{' '}
                  {result.holidays.map((holiday) => `${holiday.name} (${formatDay(holiday.date)})`).join(', ')}
                </p>
              ) : null}
              {result.problem ? <p role="alert">{result.problem}</p> : null}
            </>
          )}
        </div>
      </div>
    </Dialog>
  );
}
