import type { Role } from '@/types/api';

export const roleOptions: Role[] = ['Employee', 'Manager', 'HR', 'ProductManager'];

/** What each role can do, in plain words, shown under the role picker. */
export const roleDescriptions: Record<Role, string> = {
  Employee: 'Sees their own information, time off, documents, onboarding and projects.',
  Manager: 'Everything an employee sees, plus their team: approvals, onboarding and time off.',
  HR: 'Everything for everyone: people, contracts, salary, documents, logins, settings and audit history.',
  ProductManager: 'Runs projects and their members and documents, without access to private HR data.'
};
