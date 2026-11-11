import { Bot, CalendarDays, FileClock, KeyRound, UserCog, type LucideIcon } from 'lucide-react';
import { NavLink, Outlet } from 'react-router';

import { canManageLeave, canManageUsers, canViewAudit } from '@/app/access';
import { useAuth } from '@/features/auth/hooks/use-auth';

interface Section {
  label: string;
  to: string;
  icon: LucideIcon;
  hint: string;
}

/**
 * One place for every setting: your own account for everyone, and the company's settings for HR.
 * Opened from the profile menu (top right).
 */
export function SettingsLayout() {
  const { user } = useAuth();
  const groups: { label: string; sections: Section[] }[] = [
    {
      label: 'Your account',
      sections: [
        { label: 'Account and password', to: '/settings/account', icon: KeyRound, hint: 'Email, role, password' }
      ]
    },
    {
      label: 'Company',
      sections: [
        ...(canManageLeave(user)
          ? [
              {
                label: 'Time off and holidays',
                to: '/settings/time-off',
                icon: CalendarDays,
                hint: 'Leave types, public holidays'
              }
            ]
          : []),
        ...(canManageUsers(user)
          ? [
              { label: 'AI assistant', to: '/settings/ai', icon: Bot, hint: 'Provider, limits, usage' },
              {
                label: 'Admin accounts',
                to: '/settings/admin-accounts',
                icon: UserCog,
                hint: 'Logins that are not a person'
              }
            ]
          : []),
        ...(canViewAudit(user)
          ? [{ label: 'Audit history', to: '/settings/audit', icon: FileClock, hint: 'Who changed what, and when' }]
          : [])
      ]
    }
  ].filter((group) => group.sections.length > 0);
  // With only "Your account" (employees, managers) a section menu adds nothing.
  const showMenu = groups.reduce((count, group) => count + group.sections.length, 0) > 1;

  return (
    <div className={`settings-layout ${showMenu ? '' : 'settings-layout--single'}`}>
      {showMenu ? (
        <nav className="settings-nav" aria-label="Settings">
          <h1>Settings</h1>
          {groups.map((group) => (
            <div className="settings-nav__group" key={group.label}>
              <p>{group.label}</p>
              {group.sections.map(({ label, to, icon: Icon, hint }) => (
                <NavLink key={to} to={to}>
                  <Icon size={17} aria-hidden="true" />
                  <span>
                    <strong>{label}</strong>
                    <small>{hint}</small>
                  </span>
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
      ) : null}
      <div className="settings-content">
        <Outlet />
      </div>
    </div>
  );
}
