import { describe, expect, it } from 'vitest';

import type { LeaveBalance } from './types';
import { balanceExplanation, formatDays, formatPeriod, monthGrid, upcomingHolidays } from './utils';

const balance = (overrides: Partial<LeaveBalance>): LeaveBalance => ({
  leaveTypeId: 'v',
  leaveTypeName: 'Vacation',
  color: '#2563eb',
  deductsFromBalance: true,
  year: 2027,
  fullYearAllowance: 20,
  earnedToDate: 5,
  carriedOver: 0,
  carryOverExpired: 0,
  adjustments: 0,
  approved: 0,
  pending: 0,
  available: 5,
  ...overrides
});

describe('time off formatting', () => {
  it('formats days and periods', () => {
    expect(formatDays(1)).toBe('1 day');
    expect(formatDays(1.6667)).toBe('1.67 days');
    expect(formatPeriod('2027-03-01', '2027-03-05')).toBe('1 – 5 Mar 2027');
    expect(formatPeriod('2027-02-26', '2027-03-02')).toBe('26 Feb – 2 Mar 2027');
    expect(formatPeriod('2027-03-01', '2027-03-01')).toBe('1 Mar 2027');
  });

  it('explains carried-over days before and after they expire', () => {
    const carried = balance({ carriedOver: 7, carryOverExpiresOn: '2027-06-30' });
    expect(balanceExplanation(carried, '2027-03-01')).toBe(
      '5 days earned of 20 days · 7 days from last year, use by 30 Jun 2027'
    );
    expect(balanceExplanation({ ...carried, carryOverExpired: 2 }, '2027-07-01')).toBe(
      '5 days earned of 20 days · 2 days from last year expired'
    );
  });

  it('builds Monday-first month grids', () => {
    const weeks = monthGrid(2026, 10); // November 2026 starts on a Sunday
    expect(weeks[0]).toEqual([null, null, null, null, null, null, '2026-11-01']);
    expect(weeks.flat().filter(Boolean)).toHaveLength(30);
  });
});

describe('upcoming holidays', () => {
  it('rolls yearly holidays into the next year and skips ones that are too far away', () => {
    const holidays = [
      { id: 'ny', date: '2026-01-01', name: 'New Year', repeatsYearly: true },
      { id: 'once', date: '2026-12-24', name: 'Office closed', repeatsYearly: false },
      { id: 'summer', date: '2026-07-01', name: 'Summer day', repeatsYearly: true }
    ];
    expect(upcomingHolidays(holidays, '2026-11-11').map((item) => `${item.name} ${item.date}`)).toEqual([
      'Office closed 2026-12-24',
      'New Year 2027-01-01'
    ]);
  });
});
