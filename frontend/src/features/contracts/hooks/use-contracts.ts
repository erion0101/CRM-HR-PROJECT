import { useQuery } from '@tanstack/react-query';

import { getCompensation, getContracts } from '../api/contracts-api';

export const contractKeys = {
  employee: (employeeId: string) => ['contracts', employeeId] as const,
  compensation: (employeeId: string) => ['compensation', employeeId] as const,
};

export const useEmployeeContracts = (employeeId: string) => useQuery({ queryKey: contractKeys.employee(employeeId), queryFn: () => getContracts(employeeId), enabled: Boolean(employeeId) });
export const useEmployeeCompensation = (employeeId: string) => useQuery({ queryKey: contractKeys.compensation(employeeId), queryFn: () => getCompensation(employeeId), enabled: Boolean(employeeId) });
