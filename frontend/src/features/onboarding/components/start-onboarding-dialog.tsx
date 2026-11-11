import { useQuery } from '@tanstack/react-query';

import { assignOnboarding, getOnboardingTemplates } from '../api/onboarding-api';
import { useOnboardingMutation } from '../hooks/use-onboarding-mutation';
import { AssignmentDialog } from './assignment-dialog';

/**
 * "Start onboarding" from anywhere: loads the published packages and saves the choice.
 * Pass `employee` to start it for one person (for example from their profile).
 */
export function StartOnboardingDialog({
  open,
  employee,
  onClose
}: {
  open: boolean;
  employee?: { id: string; name: string };
  onClose: () => void;
}) {
  const packages = useQuery({
    queryKey: ['onboarding', 'templates', 'manage'],
    queryFn: () => getOnboardingTemplates(false),
    enabled: open
  });
  const start = useOnboardingMutation({
    mutationFn: assignOnboarding,
    success: { title: 'Onboarding started', message: 'They can see it under Onboarding when they sign in.' },
    errorTitle: 'Could not start onboarding',
    onSuccess: onClose
  });
  // A package with a newer draft still uses its last published version.
  const published = (packages.data ?? []).filter((pkg) => pkg.publishedVersion != null && pkg.status !== 'Retired');
  return (
    <AssignmentDialog
      // Remount per person so the form starts clean.
      key={employee?.id ?? 'any'}
      open={open}
      loading={start.isPending}
      templates={published}
      employee={employee}
      onClose={onClose}
      onSave={(value) => start.mutateAsync(value)}
    />
  );
}
