import { useMutation } from '@tanstack/react-query';

import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { toAppError } from '@/lib/api-error';

/** Mutation wrapper for time off: refreshes leave queries (and Home, which shows who is out) and shows a toast. */
export function useTimeOffMutation<TVariables, TData>({
  mutationFn,
  success,
  errorTitle,
  onSuccess
}: {
  mutationFn: (variables: TVariables) => Promise<TData>;
  success?: { title: string; message?: string };
  errorTitle: string;
  onSuccess?: (data: TData) => void | Promise<void>;
}) {
  const { notify } = useNotification();
  return useMutation({
    mutationFn,
    onSuccess: async (data) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['leave'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
        queryClient.invalidateQueries({ queryKey: ['notifications'] })
      ]);
      await onSuccess?.(data);
      if (success) notify({ title: success.title, message: success.message, tone: 'success' });
    },
    onError: (error) => notify({ title: errorTitle, message: toAppError(error).message, tone: 'error' })
  });
}
