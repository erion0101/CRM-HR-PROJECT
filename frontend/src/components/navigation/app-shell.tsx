import {
  Bot,
  Building2,
  ChevronDown,
  CircleUserRound,
  FileClock,
  LayoutDashboard,
  ListChecks,
  LogOut,
  Menu,
  Settings2,
  Sparkles,
  UserCog,
  Users,
  X
} from 'lucide-react';
import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router';

import { canManageUsers, canViewAudit, canViewCompanyWorkspace, roleLabels } from '@/app/access';
import { Avatar } from '@/components/ui/avatar';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { NotificationBell } from '@/features/notifications/components/notification-bell';

interface NavigationItem {
  label: string;
  to: string;
  icon: typeof Users;
  visible?: boolean;
  exact?: boolean;
  activePaths?: string[];
  excludePaths?: string[];
}

export function AppShell() {
  const { user, logout } = useAuth();
  const { pathname } = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const navigation: { label: string; items: NavigationItem[] }[] = [
    {
      label: 'Workspace',
      items: [
        { label: 'Home', to: '/', icon: LayoutDashboard, exact: true },
        {
          label: 'My info',
          to: user?.employeeId ? `/employees/${user.employeeId}` : '/employees',
          icon: CircleUserRound,
          visible: Boolean(user?.employeeId),
          exact: true
        },
        {
          label: 'People',
          to: '/employees',
          icon: Users,
          activePaths: ['/employees', '/organization-chart', '/departments', '/positions'],
          excludePaths: user?.employeeId ? [`/employees/${user.employeeId}`] : []
        }
      ]
    },
    {
      label: 'Work',
      items: [
        { label: 'Company', to: '/company', icon: Building2, visible: canViewCompanyWorkspace(user) },
        {
          label: 'Onboarding',
          to: '/onboarding',
          icon: ListChecks,
          activePaths: ['/onboarding', '/knowledge', '/projects']
        }
      ]
    },
    { label: 'Intelligence', items: [{ label: 'AI assistant', to: '/ai-assistant', icon: Bot }] },
    {
      label: 'Administration',
      items: [
        { label: 'User access', to: '/administration/users', icon: UserCog, visible: canManageUsers(user) },
        { label: 'Audit history', to: '/administration/audit', icon: FileClock, visible: canViewAudit(user) },
        { label: 'AI settings', to: '/administration/ai-settings', icon: Bot, visible: canManageUsers(user) }
      ]
    }
  ];

  return (
    <div className="app-shell">
      <button
        className={`sidebar-scrim ${mobileOpen ? 'is-visible' : ''}`}
        aria-label="Close navigation"
        onClick={() => setMobileOpen(false)}
      />
      <aside className={`sidebar ${mobileOpen ? 'is-open' : ''}`}>
        <div className="sidebar__brand">
          <span className="brand__mark">
            <Sparkles size={19} />
          </span>
          <span>PeopleSpring</span>
          <button className="sidebar__close" onClick={() => setMobileOpen(false)} aria-label="Close navigation">
            <X size={20} />
          </button>
        </div>
        <nav className="sidebar__nav" aria-label="Primary navigation">
          {navigation.map((group) => {
            const visible = group.items.filter((item) => item.visible !== false);
            if (!visible.length) return null;
            return (
              <div className="nav-group" key={group.label}>
                <p>{group.label}</p>
                {visible.map((item) => {
                  const { label, to, icon: Icon } = item;
                  const excluded = item.excludePaths?.includes(pathname) ?? false;
                  const active =
                    !excluded &&
                    (item.exact
                      ? pathname === to
                      : (item.activePaths?.some((path) => pathname === path || pathname.startsWith(`${path}/`)) ??
                        (pathname === to || pathname.startsWith(`${to}/`))));
                  return (
                    <NavLink
                      key={`${label}-${to}`}
                      to={to}
                      className={active ? 'active' : undefined}
                      onClick={() => setMobileOpen(false)}
                    >
                      <Icon size={18} />
                      <span>{label}</span>
                    </NavLink>
                  );
                })}
              </div>
            );
          })}
        </nav>
      </aside>
      <div className="app-frame">
        <header className="topbar">
          <button className="mobile-menu" onClick={() => setMobileOpen(true)} aria-label="Open navigation">
            <Menu size={21} />
          </button>
          <NotificationBell />
          <div className="topbar__profile">
            <button
              className="profile-trigger"
              onClick={() => setProfileOpen((value) => !value)}
              aria-expanded={profileOpen}
            >
              <Avatar name={user?.email ?? ''} size="small" />
              <span>
                <strong>{user?.email.split('@')[0]}</strong>
                <small>{user?.roles[0] ? roleLabels[user.roles[0]] : 'Team member'}</small>
              </span>
              <ChevronDown size={16} />
            </button>
            {profileOpen ? (
              <div className="profile-menu">
                <div>
                  <strong>{user?.email}</strong>
                  <small>{user?.roles.map((role) => roleLabels[role]).join(' · ')}</small>
                </div>
                <NavLink to="/account/security" onClick={() => setProfileOpen(false)}>
                  <Settings2 size={16} /> Security settings
                </NavLink>
                <button onClick={() => void logout()}>
                  <LogOut size={16} /> Sign out
                </button>
              </div>
            ) : null}
          </div>
        </header>
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
