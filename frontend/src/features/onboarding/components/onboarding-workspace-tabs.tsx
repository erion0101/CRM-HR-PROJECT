import { BarChart3, BookOpen, FolderKanban, ListChecks, Settings2 } from 'lucide-react';

import { canManageOnboarding } from '@/app/access';
import { WorkspaceTabs, type WorkspaceTab } from '@/components/navigation/workspace-tabs';
import { useAuth } from '@/features/auth/hooks/use-auth';

export function OnboardingWorkspaceTabs() {
  const { user } = useAuth();
  const items: WorkspaceTab[] = [
    { label: 'My journey', to: '/onboarding', icon: ListChecks, end: true },
    { label: 'Company knowledge', to: '/knowledge', icon: BookOpen },
    { label: 'Projects', to: '/projects', icon: FolderKanban }
  ];
  if (canManageOnboarding(user)) {
    items.push({ label: 'Manage', to: '/onboarding/manage', icon: Settings2 });
    items.push({ label: 'Insights', to: '/onboarding/insights', icon: BarChart3 });
  }
  return <WorkspaceTabs label="Onboarding workspace" items={items} />;
}
