import { http } from '@/lib/api-client';
import type { PageResult } from '@/types/api';

import type {
  AssignmentHistory,
  Employee,
  EmployeeActivityEvent,
  EmployeeInput,
  EmployeeProfile,
  EmployeeReference,
  EmploymentHistory,
  EmploymentStatus,
  ReportingLineHistory
} from '../types';

export interface EmployeeFilters {
  search?: string;
  departmentId?: string;
  status?: EmploymentStatus | '';
  cursor?: string;
  limit?: number;
}

export async function getEmployees(filters: EmployeeFilters = {}): Promise<PageResult<Employee>> {
  const { data } = await http.get<PageResult<Employee>>('/employees', { params: filters });
  return data;
}

export async function getEmployee(id: string): Promise<Employee> {
  const { data } = await http.get<Employee>(`/employees/${id}`);
  return data;
}

export async function getEmployeeProfile(id: string): Promise<EmployeeProfile> {
  const { data } = await http.get<EmployeeProfile>(`/employees/${id}/profile`);
  return data;
}

export async function getEmploymentHistory(id: string): Promise<EmploymentHistory[]> {
  const { data } = await http.get<EmploymentHistory[]>(`/employees/${id}/employment-history`);
  return data;
}

export async function getAssignmentHistory(id: string): Promise<AssignmentHistory[]> {
  const { data } = await http.get<AssignmentHistory[]>(`/employees/${id}/assignments`);
  return data;
}

export async function getReportingLineHistory(id: string): Promise<ReportingLineHistory[]> {
  const { data } = await http.get<ReportingLineHistory[]>(`/employees/${id}/reporting-lines`);
  return data;
}

export async function getDirectReports(id: string): Promise<EmployeeReference[]> {
  const { data } = await http.get<EmployeeReference[]>(`/employees/${id}/direct-reports`);
  return data;
}

export async function getEmployeeActivity(
  id: string,
  cursor?: string,
  limit = 30
): Promise<PageResult<EmployeeActivityEvent>> {
  const { data } = await http.get<PageResult<EmployeeActivityEvent>>(`/employees/${id}/activity`, {
    params: { cursor, limit }
  });
  return data;
}

export async function createEmployee(input: EmployeeInput): Promise<Employee> {
  const { data } = await http.post<Employee>('/employees', input);
  return data;
}

export async function updateEmployee(
  id: string,
  version: number,
  input: Omit<EmployeeInput, 'employeeNumber'>
): Promise<Employee> {
  const { data } = await http.put<Employee>(`/employees/${id}`, input, { headers: { 'If-Match': `"${version}"` } });
  return data;
}

export async function archiveEmployee(id: string, version: number): Promise<void> {
  await http.delete(`/employees/${id}`, { headers: { 'If-Match': `"${version}"` } });
}

export async function setEmployment(
  id: string,
  input: { status: EmploymentStatus; startDate: string; endDate?: string }
): Promise<Employee> {
  const { data } = await http.put<Employee>(`/employees/${id}/employment`, input);
  return data;
}

export async function addAssignment(
  id: string,
  input: { positionId: string; departmentId: string; effectiveFrom: string; effectiveTo?: string }
): Promise<Employee> {
  const { data } = await http.post<Employee>(`/employees/${id}/assignments`, input);
  return data;
}

export async function addReportingLine(
  id: string,
  input: { effectiveFrom: string; effectiveTo?: string }
): Promise<Employee> {
  const { data } = await http.post<Employee>(`/employees/${id}/reporting-lines`, input);
  return data;
}
