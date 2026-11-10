import { describe, expect, it } from 'vitest';

import type { CurrentUser, Role } from '@/types/api';

import {
  canManageKnowledge,
  canManageOnboarding,
  canManageOrganization,
  canManagePeople,
  canManageProjects,
  canManageUsers,
  canViewAudit,
  canViewCompanyWorkspace,
  canViewSalary
} from './access';

const user = (role: Role): CurrentUser => ({
  userId: 'user-1',
  organizationId: 'org-1',
  employeeId: null,
  email: 'person@example.local',
  roles: [role]
});

describe('role capabilities', () => {
  it('limits account administration to administrators', () => {
    expect(canManageUsers(user('HR'))).toBe(true);
    expect(canManageUsers(user('ProductManager'))).toBe(false);
  });

  it('gives HR people and compensation administration', () => {
    expect(canManagePeople(user('HR'))).toBe(true);
    expect(canViewSalary(user('HR'))).toBe(true);
    expect(canManagePeople(user('Manager'))).toBe(false);
  });

  it('aligns organization and audit access with supported backend policies', () => {
    expect(canManageOrganization(user('HR'))).toBe(true);
    expect(canViewAudit(user('HR'))).toBe(true);
    expect(canViewAudit(user('Employee'))).toBe(false);
  });

  it('allows scoped onboarding management without granting company authoring to managers', () => {
    expect(canManageOnboarding(user('Manager'))).toBe(true);
    expect(canManageKnowledge(user('Manager'))).toBe(false);
    expect(canManageKnowledge(user('ProductManager'))).toBe(false);
    expect(canManageProjects(user('ProductManager'))).toBe(true);
  });

  it('keeps the company document workspace HR-only', () => {
    expect(canViewCompanyWorkspace(user('HR'))).toBe(true);
    expect(canViewCompanyWorkspace(user('Manager'))).toBe(false);
    expect(canViewCompanyWorkspace(user('ProductManager'))).toBe(false);
    expect(canViewCompanyWorkspace(user('Employee'))).toBe(false);
  });
});
