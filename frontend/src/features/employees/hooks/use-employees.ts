import { useQuery } from '@tanstack/react-query';

import { getAssignmentHistory, getEmployee, getEmployeeActivity, getEmployeeProfile, getEmployees, getEmploymentHistory, getReportingLineHistory, type EmployeeFilters } from '../api/employees-api';

export const employeeKeys = {
  all: ['employees'] as const,
  list: (filters: EmployeeFilters) => [...employeeKeys.all, 'list', filters] as const,
  detail: (id: string) => [...employeeKeys.all, 'detail', id] as const,
  profile: (id: string) => [...employeeKeys.all, 'profile', id] as const,
  employmentHistory: (id: string) => [...employeeKeys.all, 'employment-history', id] as const,
  assignmentHistory: (id: string) => [...employeeKeys.all, 'assignment-history', id] as const,
  reportingHistory: (id: string) => [...employeeKeys.all, 'reporting-history', id] as const,
  activity: (id: string, cursor?: string) => [...employeeKeys.all, 'activity', id, { cursor }] as const,
};

export function useEmployees(filters: EmployeeFilters = {}) {
  return useQuery({ queryKey: employeeKeys.list(filters), queryFn: () => getEmployees(filters) });
}

export function useEmployee(id: string) {
  return useQuery({ queryKey: employeeKeys.detail(id), queryFn: () => getEmployee(id), enabled: Boolean(id) });
}

export function useEmployeeProfile(id: string) {
  return useQuery({ queryKey: employeeKeys.profile(id), queryFn: () => getEmployeeProfile(id), enabled: Boolean(id) });
}

export function useEmploymentHistory(id: string) {
  return useQuery({ queryKey: employeeKeys.employmentHistory(id), queryFn: () => getEmploymentHistory(id), enabled: Boolean(id) });
}

export function useAssignmentHistory(id: string) {
  return useQuery({ queryKey: employeeKeys.assignmentHistory(id), queryFn: () => getAssignmentHistory(id), enabled: Boolean(id) });
}

export function useReportingLineHistory(id: string) {
  return useQuery({ queryKey: employeeKeys.reportingHistory(id), queryFn: () => getReportingLineHistory(id), enabled: Boolean(id) });
}

export function useEmployeeActivity(id: string, cursor?: string) {
  return useQuery({ queryKey: employeeKeys.activity(id, cursor), queryFn: () => getEmployeeActivity(id, cursor), enabled: Boolean(id) });
}
