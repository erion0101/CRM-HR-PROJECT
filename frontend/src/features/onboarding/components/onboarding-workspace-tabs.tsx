import { BarChart3, BookOpen, FileText, FolderKanban, LayoutDashboard, ListChecks, Package, Users } from 'lucide-react';

import { canManageOnboarding, canManagePeople } from '@/app/access';
import { WorkspaceTabs, type WorkspaceTab } from '@/components/navigation/workspace-tabs';
import { useAuth } from '@/features/auth/hooks/use-auth';

/**
 * Tabs inside the Onboarding area. HR and managers start on the overview; employees start on their own onboarding.
 * "My onboarding" appears for anyone whose account is linked to an employee record.
 */
export function OnboardingWorkspaceTabs() {
  const { user } = useAuth();
  const manages = canManageOnboarding(user);
  const isHr = canManagePeople(user);
  const items: WorkspaceTab[] = manages
    ? [
        { label: 'Overview', to: '/onboarding', icon: LayoutDashboard, end: true },
        ...(user?.employeeId ? [{ label: 'My onboarding', to: '/onboarding/me', icon: ListChecks }] : []),
        ...(isHr
          ? [
              { label: 'Documents', to: '/company', icon: FileText },
              { label: 'Packages & quizzes', to: '/onboarding/packages', icon: Package }
            ]
          : []),
        { label: 'Progress', to: '/onboarding/progress', icon: Users },
        { label: 'Insights', to: '/onboarding/insights', icon: BarChart3 },
        { label: 'Company knowledge', to: '/knowledge', icon: BookOpen },
        { label: 'Projects', to: '/projects', icon: FolderKanban }
      ]
    : [
        { label: 'My onboarding', to: '/onboarding', icon: ListChecks, end: true },
        { label: 'Company knowledge', to: '/knowledge', icon: BookOpen },
        { label: 'Projects', to: '/projects', icon: FolderKanban }
      ];
  return <WorkspaceTabs label="Onboarding" items={items} />;
}
