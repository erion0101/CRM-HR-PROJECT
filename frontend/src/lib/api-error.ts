import axios from 'axios';

import type { ProblemDetails } from '@/types/api';

export interface AppError {
  status?: number;
  message: string;
  fieldErrors: Record<string, string[]>;
  correlationId?: string;
}

export function toAppError(error: unknown): AppError {
  if (!axios.isAxiosError<ProblemDetails>(error)) {
    return { message: 'Something went wrong. Please try again.', fieldErrors: {} };
  }

  const status = error.response?.status;
  const problem = error.response?.data;
  const fallback =
    status === 401
      ? 'Your session has ended. Please sign in again.'
      : status === 403
        ? 'You do not have permission to perform this action.'
        : status === 404
          ? 'The requested record could not be found.'
          : status && status >= 500
            ? 'The service is temporarily unavailable. Please try again.'
            : 'Unable to complete the request. Please check the form and try again.';

  return {
    status,
    message: problem?.detail || problem?.title || fallback,
    fieldErrors: problem?.errors ?? {},
    correlationId: problem?.correlationId
  };
}
