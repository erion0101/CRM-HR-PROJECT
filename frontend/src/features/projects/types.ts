export type ProjectStatus = 'Planned' | 'Active' | 'OnHold' | 'Completed' | 'Archived';

export interface ProjectMember {
  membershipId: string;
  employeeId: string;
  employeeName: string;
  projectRole: string;
  responsibilities: string;
  effectiveFrom: string;
  effectiveTo?: string;
  version: number;
}

export interface ProjectResourceLink { id: string; label: string; url: string; resourceType: string; sortOrder: number }

export interface ProjectEmployeeOption { id: string; employeeNumber: string; fullName: string; workEmail: string }

export interface Project {
  id: string;
  code: string;
  name: string;
  purpose: string;
  description: string;
  departmentId?: string;
  departmentName?: string;
  leadEmployeeId: string;
  leadEmployeeName: string;
  onboardingContactEmployeeId?: string;
  onboardingContactEmployeeName?: string;
  status: ProjectStatus;
  canManage: boolean;
  members: ProjectMember[];
  links: ProjectResourceLink[];
  version: number;
}

export interface ProjectInput {
  code: string;
  name: string;
  purpose: string;
  description: string;
  departmentId?: string;
  leadEmployeeId: string;
  onboardingContactEmployeeId?: string;
  status: ProjectStatus;
  links: { label: string; url: string; resourceType: string; sortOrder: number }[];
}
