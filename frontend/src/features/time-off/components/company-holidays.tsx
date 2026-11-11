import { useQuery } from '@tanstack/react-query';

import { getPublicHolidays } from '../api/time-off-api';
import { formatDay, upcomingHolidays } from '../utils';

/** Public holidays in the next 90 days, for the Home page. Renders `empty` when there are none. */
export function CompanyHolidays({ empty }: { empty: React.ReactNode }) {
  const holidays = useQuery({ queryKey: ['leave', 'holidays'], queryFn: getPublicHolidays });
  const upcoming = upcomingHolidays(holidays.data ?? []).slice(0, 5);
  if (!upcoming.length) return <>{empty}</>;
  return (
    <ul className="leave-requests">
      {upcoming.map((holiday) => (
        <li key={`${holiday.id}-${holiday.date}`}>
          <div className="leave-requests__main">
            <strong>{holiday.name}</strong>
            <small>{formatDay(holiday.date)}</small>
          </div>
        </li>
      ))}
    </ul>
  );
}
