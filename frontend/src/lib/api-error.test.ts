import axios from 'axios';
import { describe, expect, it } from 'vitest';

import { toAppError } from './api-error';

describe('toAppError', () => {
  it('maps RFC problem details and field validation messages', () => {
    const error = new axios.AxiosError('Bad request', '400', undefined, undefined, {
      data: { title: 'Validation failed', errors: { workEmail: ['Email is already used.'] }, correlationId: 'corr-1' },
      status: 400,
      statusText: 'Bad Request',
      headers: {},
      config: { headers: new axios.AxiosHeaders() }
    });
    expect(toAppError(error)).toEqual({
      status: 400,
      message: 'Validation failed',
      fieldErrors: { workEmail: ['Email is already used.'] },
      correlationId: 'corr-1'
    });
  });

  it('uses a safe message for unknown failures', () => {
    expect(toAppError(new Error('secret')).message).toBe('Something went wrong. Please try again.');
  });
});
