import { useMutation } from '@tanstack/react-query';

import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { toAppError } from '@/lib/api-error';

interface OnboardingMutationOptions<TVariables, TData> {
  mutationFn: (variables: TVariables) => Promise<TData>;
  /** Toast shown on success. Omit for silent mutations. */
  success?: { title: string; message?: string };
  errorTitle: string;
  /** Refetch all onboarding queries after success. Defaults to true. */
  invalidate?: boolean;
  onSuccess?: (data: TData) => void | Promise<void>;
}

/**
 * Shared mutation wrapper for the onboarding feature: refreshes onboarding queries and
 * shows consistent success and error notifications.
 */
export function useOnboardingMutation<TVariables, TData>({
  mutationFn,
  success,
  errorTitle,
  invalidate = true,
  onSuccess
}: OnboardingMutationOptions<TVariables, TData>) {
  const { notify } = useNotification();
  return useMutation({
    mutationFn,
    onSuccess: async (data) => {
      if (invalidate) await queryClient.invalidateQueries({ queryKey: ['onboarding'] });
      await onSuccess?.(data);
      if (success) notify({ title: success.title, message: success.message, tone: 'success' });
    },
    onError: (error) => notify({ title: errorTitle, message: toAppError(error).message, tone: 'error' })
  });
}
