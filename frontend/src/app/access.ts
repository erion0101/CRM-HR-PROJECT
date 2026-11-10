import type { CurrentUser, Role } from '@/types/api';

export const roleLabels: Record<Role, string> = {
  Employee: 'Employee',
  Manager: 'Manager',
  HR: 'HR',
  ProductManager: 'Product Manager',
};

export function hasAnyRole(user: CurrentUser | null, roles: Role[]): boolean {
  return Boolean(user?.roles.some((role) => roles.includes(role)));
}

export const canManagePeople = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canManageOrganization = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canViewSalary = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canManageUsers = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canViewAudit = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canManageKnowledge = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canViewCompanyWorkspace = (user: CurrentUser | null) => hasAnyRole(user, ['HR']);
export const canManageOnboarding = (user: CurrentUser | null) => hasAnyRole(user, ['HR', 'Manager']);
export const canManageProjects = (user: CurrentUser | null) => hasAnyRole(user, ['HR', 'ProductManager']);
