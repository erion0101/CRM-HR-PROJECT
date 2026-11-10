import type { Role } from '@/types/api';

export interface NewHireForm {
  firstName: string;
  lastName: string;
  workEmail: string;
  employeeNumber: string;
  phone: string;
  departmentId: string;
  positionId: string;
  startDate: string;
  createLogin: boolean;
  role: Role;
  temporaryPassword: string;
  startOnboarding: boolean;
  templateId: string;
  projectId: string;
  projectRole: string;
  mentorEmployeeId: string;
}
