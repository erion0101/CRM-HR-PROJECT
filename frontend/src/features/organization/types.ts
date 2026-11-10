export type OrganizationUnitType = 'BusinessUnit' | 'Division' | 'Department' | 'Team';

export interface Department {
  id: string;
  name: string;
  code: string | null;
  type: OrganizationUnitType;
  parentDepartmentId: string | null;
  managerEmployeeId: string | null;
  managerName: string | null;
  hrEmployeeId: string | null;
  hrName: string | null;
  productManagerEmployeeId: string | null;
  productManagerName: string | null;
  description: string | null;
  directEmployeeCount: number;
  totalEmployeeCount: number;
  childUnitCount: number;
  version: number;
}

export interface DepartmentInput {
  name: string;
  code?: string;
  type: OrganizationUnitType;
  parentDepartmentId?: string;
  managerEmployeeId?: string;
  hrEmployeeId?: string;
  productManagerEmployeeId?: string;
  description?: string;
}

export interface DepartmentEmployee {
  id: string;
  employeeNumber: string;
  fullName: string;
  workEmail: string;
  position: string | null;
  employmentStatus: 'Active' | 'OnLeave' | 'Suspended' | 'Terminated' | null;
  projects: string[];
}

export interface DepartmentOverview {
  department: Department;
  employees: DepartmentEmployee[];
}

export interface Position {
  id: string;
  departmentId: string | null;
  title: string;
  code: string | null;
  version: number;
}

export interface PositionInput {
  departmentId: string;
  title: string;
  code?: string;
}
