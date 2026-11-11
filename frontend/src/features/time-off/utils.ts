import type { LeaveBalance, LeaveRequestStatus, PublicHoliday } from './types';

const dayFormat = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', timeZone: 'UTC' });
const dayYearFormat = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC'
});

/** Parses an ISO date (yyyy-mm-dd) as a calendar day, not a moment in a time zone. */
export function parseDay(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

export function toIsoDay(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function todayIso(): string {
  const now = new Date();
  return toIsoDay(new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())));
}

/** "4 Mar 2027" for one day, "4 – 8 Mar 2027" or "28 Feb – 4 Mar 2027" for a range. */
export function formatPeriod(start: string, end: string): string {
  if (start === end) return dayYearFormat.format(parseDay(start));
  const a = parseDay(start);
  const b = parseDay(end);
  if (a.getUTCMonth() === b.getUTCMonth() && a.getUTCFullYear() === b.getUTCFullYear())
    return `${a.getUTCDate()} – ${dayYearFormat.format(b)}`;
  return `${dayFormat.format(a)} – ${dayYearFormat.format(b)}`;
}

export function formatDay(value: string): string {
  return dayYearFormat.format(parseDay(value));
}

/** Days with at most two decimals, without trailing zeros: 20, 1.67, 7.5. */
export function formatDays(value: number): string {
  const rounded = Math.round(value * 100) / 100;
  return `${rounded} ${Math.abs(rounded) === 1 ? 'day' : 'days'}`;
}

export const statusTone: Record<LeaveRequestStatus, 'neutral' | 'positive' | 'warning' | 'negative' | 'info'> = {
  Pending: 'warning',
  Approved: 'positive',
  Rejected: 'negative',
  Cancelled: 'neutral'
};

export const statusLabel: Record<LeaveRequestStatus, string> = {
  Pending: 'Waiting for approval',
  Approved: 'Approved',
  Rejected: 'Not approved',
  Cancelled: 'Cancelled'
};

/** One short line under a balance: where the days come from and when carried-over days run out. */
export function balanceExplanation(balance: LeaveBalance, today = todayIso()): string {
  if (!balance.deductsFromBalance) return `${formatDays(balance.approved)} taken this year`;
  const parts = [`${formatDays(balance.earnedToDate)} earned of ${formatDays(balance.fullYearAllowance)}`];
  if (balance.carriedOver > 0) {
    const expiry = balance.carryOverExpiresOn;
    if (expiry && expiry < today) {
      if (balance.carryOverExpired > 0) parts.push(`${formatDays(balance.carryOverExpired)} from last year expired`);
    } else {
      parts.push(`${formatDays(balance.carriedOver)} from last year${expiry ? `, use by ${formatDay(expiry)}` : ''}`);
    }
  }
  if (balance.adjustments)
    parts.push(`${balance.adjustments > 0 ? '+' : ''}${formatDays(balance.adjustments)} adjusted by HR`);
  return parts.join(' · ');
}

/** Monday-first weeks covering the whole month, as ISO days; days outside the month are null. */
export function monthGrid(year: number, month: number): (string | null)[][] {
  const first = new Date(Date.UTC(year, month, 1));
  const daysInMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  const offset = (first.getUTCDay() + 6) % 7;
  const cells: (string | null)[] = Array.from({ length: offset }, () => null);
  for (let day = 1; day <= daysInMonth; day++) cells.push(toIsoDay(new Date(Date.UTC(year, month, day))));
  while (cells.length % 7) cells.push(null);
  const weeks: (string | null)[][] = [];
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
  return weeks;
}

export function isWeekend(day: string): boolean {
  const weekday = parseDay(day).getUTCDay();
  return weekday === 0 || weekday === 6;
}

/** Holidays in the next `days` days (today included), using each yearly holiday's date in the right year. */
export function upcomingHolidays(holidays: PublicHoliday[], today = todayIso(), days = 90): PublicHoliday[] {
  const start = parseDay(today);
  const end = toIsoDay(new Date(start.getTime() + days * 86_400_000));
  const years = [start.getUTCFullYear(), start.getUTCFullYear() + 1];
  return holidays
    .flatMap((holiday) =>
      holiday.repeatsYearly
        ? years
            .filter((year) => year >= Number(holiday.date.slice(0, 4)))
            .map((year) => ({ ...holiday, date: `${year}${holiday.date.slice(4)}` }))
            // 29 February only exists in leap years.
            .filter((item) => !item.date.endsWith('-02-29') || toIsoDay(parseDay(item.date)) === item.date)
        : [holiday]
    )
    .filter((holiday) => holiday.date >= today && holiday.date <= end)
    .sort((a, b) => a.date.localeCompare(b.date));
}
