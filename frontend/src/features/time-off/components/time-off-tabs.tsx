import { CalendarDays, CalendarCheck, Inbox } from 'lucide-react';

import { canApproveLeave } from '@/app/access';
import { WorkspaceTabs, type WorkspaceTab } from '@/components/navigation/workspace-tabs';
import { useAuth } from '@/features/auth/hooks/use-auth';

/** Tabs inside Time off. "My time off" needs an employee profile; Approvals is for managers and HR. Leave rules are in Settings. */
export function TimeOffTabs() {
  const { user } = useAuth();
  const items: WorkspaceTab[] = [
    ...(user?.employeeId ? [{ label: 'My time off', to: '/time-off', icon: CalendarCheck, end: true }] : []),
    ...(canApproveLeave(user) ? [{ label: 'Approvals', to: '/time-off/approvals', icon: Inbox }] : []),
    { label: 'Team calendar', to: '/time-off/calendar', icon: CalendarDays }
  ];
  return <WorkspaceTabs label="Time off" items={items} />;
}
