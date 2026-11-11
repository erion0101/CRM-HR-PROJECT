import { useQuery } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { canManageLeave } from '@/app/access';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Select } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { useDepartments } from '@/features/organization/hooks/use-organization';

import { getLeaveCalendar } from '../api/time-off-api';
import { TimeOffTabs } from '../components/time-off-tabs';
import { formatDay, formatPeriod, isWeekend, monthGrid, todayIso, toIsoDay } from '../utils';

const monthName = new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric', timeZone: 'UTC' });
const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

/** A month of who is out and public holidays. HR can look at one department; others see their own team. */
export function TeamCalendarPage() {
  const { user } = useAuth();
  const isHr = canManageLeave(user);
  const now = new Date();
  const [month, setMonth] = useState({ year: now.getFullYear(), month: now.getMonth() });
  const [departmentId, setDepartmentId] = useState('');
  const departments = useDepartments(undefined, 100);
  const from = toIsoDay(new Date(Date.UTC(month.year, month.month, 1)));
  const to = toIsoDay(new Date(Date.UTC(month.year, month.month + 1, 0)));
  const calendar = useQuery({
    queryKey: ['leave', 'calendar', from, to, departmentId],
    queryFn: () => getLeaveCalendar(from, to, departmentId || undefined)
  });
  const move = (delta: number) =>
    setMonth(({ year, month: m }) => {
      const next = new Date(Date.UTC(year, m + delta, 1));
      return { year: next.getUTCFullYear(), month: next.getUTCMonth() };
    });
  const today = todayIso();
  const holidays = new Map((calendar.data?.holidays ?? []).map((holiday) => [holiday.date, holiday.name]));
  const entries = calendar.data?.entries ?? [];
  const outOn = (day: string) => entries.filter((entry) => entry.startDate <= day && entry.endDate >= day);

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Time off"
        title="Team calendar"
        description={isHr ? 'Who is out, and public holidays.' : 'Who in your team is out, and public holidays.'}
      >
        <TimeOffTabs />
      </PageHeader>

      <div className="progress-toolbar">
        <div className="leave-month-nav">
          <Button variant="secondary" size="small" onClick={() => move(-1)} aria-label="Previous month">
            <ChevronLeft size={16} />
          </Button>
          <h2>{monthName.format(new Date(Date.UTC(month.year, month.month, 1)))}</h2>
          <Button variant="secondary" size="small" onClick={() => move(1)} aria-label="Next month">
            <ChevronRight size={16} />
          </Button>
        </div>
        {isHr ? (
          <Select
            aria-label="Department"
            value={departmentId}
            onChange={(event) => setDepartmentId(event.target.value)}
          >
            <option value="">Everyone</option>
            {(departments.data?.items ?? []).map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </Select>
        ) : null}
      </div>

      {calendar.isLoading ? (
        <Card>
          <Skeleton count={6} />
        </Card>
      ) : calendar.isError ? (
        <ErrorState onRetry={() => void calendar.refetch()} />
      ) : (
        <>
          <Card className="leave-calendar">
            <div className="leave-calendar__grid" role="grid" aria-label="Month">
              {weekdays.map((weekday) => (
                <div className="leave-calendar__weekday" role="columnheader" key={weekday}>
                  {weekday}
                </div>
              ))}
              {monthGrid(month.year, month.month)
                .flat()
                .map((day, index) => {
                  if (!day)
                    return <div className="leave-calendar__day leave-calendar__day--empty" key={`empty-${index}`} />;
                  const out = outOn(day);
                  const holiday = holidays.get(day);
                  const classes = [
                    'leave-calendar__day',
                    isWeekend(day) ? 'leave-calendar__day--weekend' : '',
                    holiday ? 'leave-calendar__day--holiday' : '',
                    day === today ? 'leave-calendar__day--today' : ''
                  ].join(' ');
                  return (
                    <div
                      className={classes}
                      role="gridcell"
                      key={day}
                      title={
                        [holiday, ...out.map((entry) => entry.employeeName)].filter(Boolean).join(', ') || undefined
                      }
                    >
                      <span className="leave-calendar__date">{Number(day.slice(8))}</span>
                      {holiday ? <span className="leave-calendar__holiday">{holiday}</span> : null}
                      {!isWeekend(day) && !holiday && out.length ? (
                        <span className="leave-calendar__count">{out.length} out</span>
                      ) : null}
                    </div>
                  );
                })}
            </div>
          </Card>

          <Card>
            <div className="section-heading section-heading--padded">
              <h2>Out this month</h2>
            </div>
            {entries.length ? (
              <ul className="leave-requests">
                {entries.map((entry) => (
                  <li key={`${entry.employeeId}-${entry.startDate}`}>
                    <span
                      className="leave-requests__swatch"
                      style={{ background: entry.color ?? '#94a3b8' }}
                      aria-hidden="true"
                    />
                    <div className="leave-requests__main">
                      <strong>{entry.employeeName}</strong>
                      <small>
                        {formatPeriod(entry.startDate, entry.endDate)}
                        {entry.leaveTypeName ? ` · ${entry.leaveTypeName}` : ''}
                      </small>
                    </div>
                    {entry.status === 'Pending' ? <Badge tone="warning">Not approved yet</Badge> : null}
                  </li>
                ))}
              </ul>
            ) : (
              <div className="state-panel">
                <h3>Nobody is out this month</h3>
                <p>Approved time off appears here.</p>
              </div>
            )}
            {calendar.data!.holidays.length ? (
              <p className="leave-calendar__holiday-list">
                Public holidays:{' '}
                {calendar.data!.holidays.map((holiday) => `${holiday.name} (${formatDay(holiday.date)})`).join(', ')}
              </p>
            ) : null}
          </Card>
        </>
      )}
    </div>
  );
}
