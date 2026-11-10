import { ArrowRight, BookOpen, Bot, Cake, CalendarDays, Link2, ListChecks, PartyPopper, UserPlus, Users } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router';

import { canManagePeople } from '@/app/access';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Avatar } from '@/components/ui/avatar';
import { Card } from '@/components/ui/card';
import { useAuth } from '@/features/auth/hooks/use-auth';

import type { DashboardCelebration, DashboardEmployee } from '../types';
import { useDashboardSummary } from '../hooks/use-dashboard';

const companyLinks = [
  { label: 'Company knowledge', description: 'Policies, culture, and internal guidance', to: '/knowledge', icon: BookOpen },
  { label: 'Employee directory', description: 'Find colleagues and organization details', to: '/employees', icon: Users },
  { label: 'My onboarding', description: 'Continue your assigned learning journey', to: '/onboarding', icon: ListChecks },
  { label: 'AI assistant', description: 'Ask questions using approved company knowledge', to: '/ai-assistant', icon: Bot },
];

export function DashboardPage() {
  const { user } = useAuth();
  const summary = useDashboardSummary();

  if (summary.isLoading) return <div className="home-dashboard-grid" aria-label="Loading home dashboard">{Array.from({ length: 5 }, (_, index) => <Card className="home-widget home-widget--loading" key={index}><Skeleton count={5} /></Card>)}</div>;
  if (summary.isError || !summary.data) return <Card><ErrorState title="Home could not be loaded" description="The people dashboard is temporarily unavailable." onRetry={() => void summary.refetch()} /></Card>;

  const data = summary.data;
  const links = canManagePeople(user)
    ? [...companyLinks, { label: 'Add an employee', description: 'Create and configure a new employee record', to: '/employees/new', icon: UserPlus }]
    : companyLinks;

  return <div className="home-dashboard-grid">
    <HomeWidget title="Celebrations" icon={<PartyPopper size={19} />}>
      {data.upcomingCelebrations.length
        ? <div className="home-people-list">{data.upcomingCelebrations.map((celebration) => <CelebrationRow celebration={celebration} key={`${celebration.employeeId}-${celebration.type}`} />)}</div>
        : <CompactEmpty icon={<PartyPopper size={28} />} title="No celebrations coming up" description="Birthdays and work anniversaries in the next 90 days will appear here." />}
    </HomeWidget>

    <HomeWidget title="Who's out" icon={<CalendarDays size={19} />}>
      <div className="home-widget__today"><strong>Today</strong><span>{longDate(new Date())}</span></div>
      {data.peopleOnLeave.length
        ? <div className="home-people-list">{data.peopleOnLeave.map((employee) => <EmployeeRow employee={employee} detail="Currently marked on leave" key={employee.id} />)}</div>
        : <CompactEmpty icon={<CalendarDays size={28} />} title="Everyone is in" description="Nobody in your access scope is marked as on leave today." />}
    </HomeWidget>

    <HomeWidget title="Company links" icon={<Link2 size={19} />}>
      <nav className="home-company-links" aria-label="Company links">{links.map(({ label, description, to, icon: Icon }) => <Link to={to} key={to}><span><Icon size={17} /></span><div><strong>{label}</strong><small>{description}</small></div><ArrowRight size={15} /></Link>)}</nav>
    </HomeWidget>

    <HomeWidget title="Welcome to the team" icon={<Users size={19} />} className="home-widget--welcome">
      {data.recentEmployees.length
        ? <div className="welcome-team"><span className="welcome-team__icon"><PartyPopper size={30} /></span><div><h2>Meet our newest team members</h2><p>Say hello and help them feel at home.</p></div><div className="welcome-team__people">{data.recentEmployees.slice(0, 4).map((employee) => <Link to={`/employees/${employee.id}`} key={employee.id}><Avatar name={employee.fullName} size="large" /><strong>{employee.fullName}</strong><small>{employee.position ?? employee.organizationUnit ?? 'Team member'}</small>{employee.hireDate ? <span className="welcome-team__date">Joined {shortDate(employee.hireDate)}</span> : null}</Link>)}</div></div>
        : <CompactEmpty icon={<Users size={28} />} title="No new team members yet" description="Recently added employees will be introduced here." />}
    </HomeWidget>

    <HomeWidget title="Company holidays" icon={<PartyPopper size={19} />}>
      <CompactEmpty icon={<CalendarDays size={28} />} title="No holidays configured" description="Company holiday dates will appear here when the holiday calendar module is configured." />
    </HomeWidget>
  </div>;
}

function HomeWidget({ title, icon, className = '', children }: { title: string; icon: ReactNode; className?: string; children: ReactNode }) {
  return <Card className={`home-widget ${className}`}><header><span>{icon}</span><h2>{title}</h2></header><div className="home-widget__content">{children}</div></Card>;
}

function CelebrationRow({ celebration }: { celebration: DashboardCelebration }) {
  const birthday = celebration.type === 'Birthday';
  const detail = birthday ? 'Happy birthday!' : `${ordinal(celebration.years ?? 1)} work anniversary`;
  return <Link className="home-person-row" to={`/employees/${celebration.employeeId}`}><Avatar name={celebration.fullName} /><div><strong>{celebration.fullName}</strong><small>{shortDate(celebration.date)} · {detail}</small></div><span className="home-person-row__event">{birthday ? <Cake size={19} /> : <CalendarDays size={19} />}</span></Link>;
}

function EmployeeRow({ employee, detail }: { employee: DashboardEmployee; detail: string }) {
  return <Link className="home-person-row" to={`/employees/${employee.id}`}><Avatar name={employee.fullName} /><div><strong>{employee.fullName}</strong><small>{employee.position ?? employee.organizationUnit ?? detail}</small><span>{detail}</span></div></Link>;
}

function CompactEmpty({ icon, title, description }: { icon: ReactNode; title: string; description: string }) {
  return <div className="home-compact-empty"><span>{icon}</span><strong>{title}</strong><p>{description}</p></div>;
}

function parseDate(value: string) {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function shortDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { month: 'long', day: 'numeric' }).format(parseDate(value));
}

function longDate(value: Date) {
  return new Intl.DateTimeFormat(undefined, { weekday: 'long', month: 'long', day: 'numeric' }).format(value);
}

function ordinal(value: number) {
  const mod100 = value % 100;
  if (mod100 >= 11 && mod100 <= 13) return `${value}th`;
  return `${value}${value % 10 === 1 ? 'st' : value % 10 === 2 ? 'nd' : value % 10 === 3 ? 'rd' : 'th'}`;
}
