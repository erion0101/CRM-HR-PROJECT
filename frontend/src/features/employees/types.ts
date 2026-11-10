export type EmploymentStatus = 'Active' | 'OnLeave' | 'Suspended' | 'Terminated';

export interface Employee {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  phone: string | null;
  dateOfBirth: string | null;
  status: EmploymentStatus | null;
  departmentId: string | null;
  positionId: string | null;
  managerId: string | null;
  version: number;
}

export interface EmployeeInput {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  phone?: string;
  dateOfBirth?: string;
}

export interface OrganizationReference {
  id: string;
  name: string;
  code: string | null;
}
export interface EmployeeReference {
  id: string;
  employeeNumber: string;
  fullName: string;
  workEmail: string;
}

export interface EmployeeProfile {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  phone: string | null;
  dateOfBirth: string | null;
  status: EmploymentStatus | null;
  hireDate: string | null;
  employmentEndDate: string | null;
  department: OrganizationReference | null;
  organizationPath: OrganizationReference[];
  position: OrganizationReference | null;
  manager: EmployeeReference | null;
  directReports: EmployeeReference[];
  version: number;
}

export interface EmploymentHistory {
  id: string;
  status: EmploymentStatus;
  startDate: string;
  endDate: string | null;
  version: number;
}
export interface AssignmentHistory {
  id: string;
  position: OrganizationReference;
  department: OrganizationReference;
  effectiveFrom: string;
  effectiveTo: string | null;
  version: number;
}
export interface ReportingLineHistory {
  id: string;
  manager: EmployeeReference;
  effectiveFrom: string;
  effectiveTo: string | null;
  version: number;
}

export interface EmployeeActivityEvent {
  id: string;
  occurredAtUtc: string;
  userId: string | null;
  employeeId: string | null;
  actorEmail: string | null;
  action: string;
  entityType: string;
  entityId: string;
  correlationId: string | null;
  ipAddress: string | null;
}
