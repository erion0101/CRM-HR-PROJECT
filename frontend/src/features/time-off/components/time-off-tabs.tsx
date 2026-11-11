import { CalendarDays, CalendarCheck, Inbox, Settings } from 'lucide-react';

import { canApproveLeave, canManageLeave } from '@/app/access';
import { WorkspaceTabs, type WorkspaceTab } from '@/components/navigation/workspace-tabs';
import { useAuth } from '@/features/auth/hooks/use-auth';

/** Tabs inside Time off. "My time off" needs an employee profile; Approvals is for managers and HR; Settings for HR. */
export function TimeOffTabs() {
  const { user } = useAuth();
  const items: WorkspaceTab[] = [
    ...(user?.employeeId ? [{ label: 'My time off', to: '/time-off', icon: CalendarCheck, end: true }] : []),
    ...(canApproveLeave(user) ? [{ label: 'Approvals', to: '/time-off/approvals', icon: Inbox }] : []),
    { label: 'Team calendar', to: '/time-off/calendar', icon: CalendarDays },
    ...(canManageLeave(user) ? [{ label: 'Leave settings', to: '/time-off/settings', icon: Settings }] : [])
  ];
  return <WorkspaceTabs label="Time off" items={items} />;
}
