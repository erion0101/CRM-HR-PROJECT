import {
  Bot,
  CalendarDays,
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

import { canApproveLeave, canManageUsers, canViewAudit, roleLabels } from '@/app/access';
import { Avatar } from '@/components/ui/avatar';
import { AiPanel } from '@/features/ai-assistant/components/ai-panel';
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
  const [aiOpen, setAiOpenState] = useState(readAiPanelOpen);
  const setAiOpen = (open: boolean) => {
    setAiOpenState(open);
    rememberAiPanelOpen(open);
  };
  // The full AI page already shows the chat, so the side panel stays closed there.
  const onAiPage = pathname === '/ai-assistant' || pathname.startsWith('/ai-assistant/');
  const showAiPanel = aiOpen && !onAiPage;
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
        }
      ]
    },
    {
      label: 'People',
      items: [
        {
          label: 'People',
          to: '/employees',
          icon: Users,
          // New hire is opened from the People page, so it highlights People.
          activePaths: ['/employees', '/organization-chart', '/departments', '/positions'],
          excludePaths: user?.employeeId ? [`/employees/${user.employeeId}`] : []
        }
      ]
    },
    {
      label: 'Onboarding',
      items: [
        {
          label: 'Onboarding',
          to: '/onboarding',
          icon: ListChecks,
          // Documents is a tab inside Onboarding, so it highlights Onboarding.
          activePaths: ['/onboarding', '/knowledge', '/projects', '/company']
        }
      ]
    },
    {
      label: 'Time off',
      items: [
        {
          label: 'Time off',
          // People without an employee profile (such as the HR admin) have no own time off; they start on approvals.
          to: user?.employeeId ? '/time-off' : canApproveLeave(user) ? '/time-off/approvals' : '/time-off/calendar',
          icon: CalendarDays,
          activePaths: ['/time-off']
        }
      ]
    },
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
    <div className={`app-shell ${showAiPanel ? 'app-shell--ai-open' : ''}`}>
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
          <button
            type="button"
            className={`ai-trigger ${showAiPanel || onAiPage ? 'is-active' : ''}`}
            aria-label="AI assistant"
            aria-expanded={showAiPanel}
            title="AI assistant"
            onClick={() => (onAiPage ? undefined : setAiOpen(!aiOpen))}
          >
            <Bot size={18} />
            <span>Ask AI</span>
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
      {showAiPanel ? <AiPanel onClose={() => setAiOpen(false)} /> : null}
    </div>
  );
}

const aiPanelKey = 'hr.ai-panel-open';

/** Whether the AI panel was open last time; a per-browser convenience, so storage errors are ignored. */
function readAiPanelOpen(): boolean {
  try {
    return window.localStorage.getItem(aiPanelKey) === '1';
  } catch {
    return false;
  }
}

function rememberAiPanelOpen(open: boolean) {
  try {
    window.localStorage.setItem(aiPanelKey, open ? '1' : '0');
  } catch {
    // Private windows can block storage; the panel still works for this visit.
  }
}
