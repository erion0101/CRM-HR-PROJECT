import { http } from '@/lib/api-client';

import type {
  EmployeeLeave,
  LeaveApproval,
  LeaveCalendar,
  LeavePreview,
  LeaveRequest,
  LeaveType,
  LeaveTypeInput,
  PublicHoliday
} from '../types';

export async function getLeaveTypes(includeArchived = false): Promise<LeaveType[]> {
  const { data } = await http.get<LeaveType[]>('/leave/types', { params: { includeArchived } });
  return data;
}

export async function createLeaveType(input: LeaveTypeInput): Promise<LeaveType> {
  const { data } = await http.post<LeaveType>('/leave/types', input);
  return data;
}

export async function updateLeaveType(id: string, version: number, input: LeaveTypeInput): Promise<LeaveType> {
  const { data } = await http.put<LeaveType>(`/leave/types/${id}`, input, { headers: { 'If-Match': `"${version}"` } });
  return data;
}

export async function archiveLeaveType(id: string, version: number): Promise<void> {
  await http.delete(`/leave/types/${id}`, { headers: { 'If-Match': `"${version}"` } });
}

export async function getPublicHolidays(): Promise<PublicHoliday[]> {
  const { data } = await http.get<PublicHoliday[]>('/leave/holidays');
  return data;
}

export async function createPublicHoliday(input: Omit<PublicHoliday, 'id'>): Promise<PublicHoliday> {
  const { data } = await http.post<PublicHoliday>('/leave/holidays', input);
  return data;
}

export async function deletePublicHoliday(id: string): Promise<void> {
  await http.delete(`/leave/holidays/${id}`);
}

export async function getMyLeave(year?: number): Promise<EmployeeLeave> {
  const { data } = await http.get<EmployeeLeave>('/leave/me', { params: { year } });
  return data;
}

export async function previewLeave(leaveTypeId: string, startDate: string, endDate: string): Promise<LeavePreview> {
  const { data } = await http.get<LeavePreview>('/leave/me/preview', { params: { leaveTypeId, startDate, endDate } });
  return data;
}

export async function requestLeave(input: {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  reason?: string;
}): Promise<LeaveRequest> {
  const { data } = await http.post<LeaveRequest>('/leave/requests', input);
  return data;
}

export async function cancelLeave(id: string): Promise<LeaveRequest> {
  const { data } = await http.post<LeaveRequest>(`/leave/requests/${id}/cancel`);
  return data;
}

export async function getLeaveApprovals(): Promise<LeaveApproval[]> {
  const { data } = await http.get<LeaveApproval[]>('/leave/approvals');
  return data;
}

export async function decideLeave(id: string, approve: boolean, comment?: string): Promise<LeaveRequest> {
  const { data } = await http.post<LeaveRequest>(`/leave/requests/${id}/${approve ? 'approve' : 'reject'}`, {
    comment
  });
  return data;
}

export async function getLeaveCalendar(from: string, to: string, departmentId?: string): Promise<LeaveCalendar> {
  const { data } = await http.get<LeaveCalendar>('/leave/calendar', { params: { from, to, departmentId } });
  return data;
}

export async function getEmployeeLeave(employeeId: string, year?: number): Promise<EmployeeLeave> {
  const { data } = await http.get<EmployeeLeave>(`/leave/employees/${employeeId}`, { params: { year } });
  return data;
}

export async function adjustLeaveBalance(
  employeeId: string,
  input: { leaveTypeId: string; year: number; days: number; reason: string }
): Promise<EmployeeLeave> {
  const { data } = await http.post<EmployeeLeave>(`/leave/employees/${employeeId}/adjustments`, input);
  return data;
}
