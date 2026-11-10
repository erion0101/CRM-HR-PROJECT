import type { EmployeeInput } from '../types';

export function normalizeEmployeeInput(input: EmployeeInput): EmployeeInput {
  return {
    ...input,
    phone: input.phone?.trim() || undefined,
    dateOfBirth: input.dateOfBirth || undefined
  };
}
