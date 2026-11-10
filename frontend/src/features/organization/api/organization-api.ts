import { http } from '@/lib/api-client';
import type { PageResult } from '@/types/api';

import type { Department, DepartmentInput, DepartmentOverview, Position, PositionInput } from '../types';

export async function getDepartments(cursor?: string, limit = 100): Promise<PageResult<Department>> {
  const { data } = await http.get<PageResult<Department>>('/departments', { params: { cursor, limit } });
  return data;
}

export async function getCurrentDepartment(departmentId?: string): Promise<DepartmentOverview> {
  const { data } = await http.get<DepartmentOverview>('/departments/current', { params: { departmentId } });
  return data;
}

export async function createDepartment(input: DepartmentInput): Promise<Department> {
  const { data } = await http.post<Department>('/departments', input);
  return data;
}

export async function updateDepartment(item: Department, input: DepartmentInput): Promise<Department> {
  const { data } = await http.put<Department>(`/departments/${item.id}`, input, {
    headers: { 'If-Match': `"${item.version}"` }
  });
  return data;
}

export async function archiveDepartment(item: Department): Promise<void> {
  await http.delete(`/departments/${item.id}`, { headers: { 'If-Match': `"${item.version}"` } });
}

export async function assignDepartmentEmployee(
  departmentId: string,
  employeeId: string,
  input: { positionId: string; effectiveFrom: string }
): Promise<DepartmentOverview> {
  const { data } = await http.put<DepartmentOverview>(`/departments/${departmentId}/employees/${employeeId}`, {
    departmentId,
    employeeId,
    ...input
  });
  return data;
}

export async function removeDepartmentEmployee(
  departmentId: string,
  employeeId: string,
  effectiveOn: string
): Promise<DepartmentOverview> {
  const { data } = await http.delete<DepartmentOverview>(`/departments/${departmentId}/employees/${employeeId}`, {
    params: { effectiveOn }
  });
  return data;
}

export async function getPositions(cursor?: string, limit = 100): Promise<PageResult<Position>> {
  const { data } = await http.get<PageResult<Position>>('/positions', { params: { cursor, limit } });
  return data;
}

export async function createPosition(input: PositionInput): Promise<Position> {
  const { data } = await http.post<Position>('/positions', input);
  return data;
}

export async function updatePosition(item: Position, input: PositionInput): Promise<Position> {
  const { data } = await http.put<Position>(`/positions/${item.id}`, input, {
    headers: { 'If-Match': `"${item.version}"` }
  });
  return data;
}

export async function archivePosition(item: Position): Promise<void> {
  await http.delete(`/positions/${item.id}`, { headers: { 'If-Match': `"${item.version}"` } });
}
