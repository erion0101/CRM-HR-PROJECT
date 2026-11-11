import { canManageOnboarding } from '@/app/access';
import { useAuth } from '@/features/auth/hooks/use-auth';

import { OnboardingPage } from './onboarding-page';
import { OnboardingOverviewPage } from './onboarding-overview-page';

/** "Onboarding" in the menu: HR and managers get the overview; everyone else gets their own onboarding. */
export function OnboardingHomePage() {
  const { user } = useAuth();
  return canManageOnboarding(user) ? <OnboardingOverviewPage /> : <OnboardingPage />;
}
