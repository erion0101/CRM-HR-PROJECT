import { BriefcaseBusiness, Building2, CalendarDays, Cake, Mail, Network, Phone, UsersRound } from 'lucide-react';
import { Link } from 'react-router';

import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';

import type { EmployeeProfile } from '../types';

const displayDate = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(`${value}T00:00:00`))
    : 'Not recorded';

function tenureSince(value: string | null): string {
  if (!value) return 'Tenure unavailable';
  const start = new Date(`${value}T00:00:00`);
  const now = new Date();
  let months = (now.getFullYear() - start.getFullYear()) * 12 + now.getMonth() - start.getMonth();
  if (now.getDate() < start.getDate()) months--;
  if (months < 0) return 'Starts in the future';
  const years = Math.floor(months / 12);
  const remainingMonths = months % 12;
  return (
    [years ? `${years}y` : '', remainingMonths ? `${remainingMonths}m` : ''].filter(Boolean).join(' ') ||
    'Less than one month'
  );
}

export function EmployeeProfileSummary({ profile }: { profile: EmployeeProfile }) {
  const fullName = `${profile.firstName} ${profile.lastName}`;
  const organizationPath = profile.organizationPath.map((unit) => unit.name).join(' / ');
  return (
    <Card className="profile-summary">
      <div className="profile-summary__identity">
        <Avatar name={fullName} size="large" />
        <div>
          <h1>{fullName}</h1>
          <span className="mono">{profile.employeeNumber}</span>
        </div>
        <Badge
          tone={profile.status === 'Active' ? 'positive' : profile.status === 'Terminated' ? 'negative' : 'warning'}
        >
          {profile.status ?? 'Not set'}
        </Badge>
      </div>
      <section className="profile-summary__section">
        <h2>Vitals</h2>
        <dl className="profile-vitals">
          <div>
            <Mail size={16} />
            <dd>{profile.workEmail}</dd>
          </div>
          <div>
            <Phone size={16} />
            <dd>{profile.phone ?? 'No phone number'}</dd>
          </div>
          <div>
            <Cake size={16} />
            <dd>{displayDate(profile.dateOfBirth)}</dd>
          </div>
          <div>
            <BriefcaseBusiness size={16} />
            <dd>{profile.position?.name ?? 'Position not assigned'}</dd>
          </div>
          <div>
            <Building2 size={16} />
            <dd title={organizationPath}>{organizationPath || 'Organization unit not assigned'}</dd>
          </div>
        </dl>
      </section>
      <section className="profile-summary__section">
        <h2>Hire date</h2>
        <div className="profile-summary__fact">
          <CalendarDays size={17} />
          <div>
            <strong>{displayDate(profile.hireDate)}</strong>
            <small>{tenureSince(profile.hireDate)}</small>
          </div>
        </div>
      </section>
      {profile.manager ? (
        <section className="profile-summary__section">
          <h2>Manager</h2>
          <Link className="profile-summary__person" to={`/employees/${profile.manager.id}`}>
            <Avatar name={profile.manager.fullName} size="small" />
            <span>
              <strong>{profile.manager.fullName}</strong>
              <small>{profile.manager.workEmail}</small>
            </span>
          </Link>
        </section>
      ) : null}
      <section className="profile-summary__section">
        <h2>Direct reports</h2>
        {profile.directReports.length ? (
          <div className="direct-report-list">
            {profile.directReports.slice(0, 5).map((employee) => (
              <Link to={`/employees/${employee.id}`} key={employee.id}>
                <Avatar name={employee.fullName} size="small" />
                <span>{employee.fullName}</span>
              </Link>
            ))}
            {profile.directReports.length > 5 ? <small>+{profile.directReports.length - 5} more</small> : null}
          </div>
        ) : (
          <div className="profile-summary__empty">
            <UsersRound size={17} /> No direct reports
          </div>
        )}
        <Link className="profile-org-link" to={`/organization-chart?employee=${profile.id}`}>
          <Network size={15} /> View in organization chart
        </Link>
      </section>
    </Card>
  );
}
