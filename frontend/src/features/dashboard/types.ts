import type { EmploymentStatus } from '@/features/employees/types';

export interface DashboardEmployee {
  id: string;
  employeeNumber: string;
  fullName: string;
  workEmail: string;
  status: EmploymentStatus | null;
  position: string | null;
  organizationUnit: string | null;
  hireDate: string | null;
}

export interface DashboardCelebration {
  employeeId: string;
  fullName: string;
  type: 'Birthday' | 'WorkAnniversary';
  date: string;
  years: number | null;
}

export interface DashboardSummary {
  visibleEmployees: number;
  activeEmployees: number;
  onLeaveEmployees: number;
  setupRequiredEmployees: number;
  activeOrganizationUnits: number;
  recentEmployees: DashboardEmployee[];
  upcomingCelebrations: DashboardCelebration[];
  peopleOnLeave: DashboardEmployee[];
}
