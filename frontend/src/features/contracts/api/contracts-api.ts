import { http } from '@/lib/api-client';

import type { Compensation, Contract } from '../types';

export async function getContracts(employeeId: string): Promise<Contract[]> {
  const { data } = await http.get<Contract[]>(`/employees/${employeeId}/contracts`);
  return data;
}

export async function createContract(employeeId: string, input: { contractNumber: string; startDate: string; endDate?: string; notes?: string }): Promise<Contract> {
  const { data } = await http.post<Contract>(`/employees/${employeeId}/contracts`, input);
  return data;
}

export async function activateContract(employeeId: string, contract: Contract): Promise<Contract> {
  const { data } = await http.post<Contract>(`/employees/${employeeId}/contracts/${contract.id}/activate`, {}, { headers: { 'If-Match': `"${contract.version}"` } });
  return data;
}

export async function terminateContract(employeeId: string, contract: Contract, terminationDate: string): Promise<Contract> {
  const { data } = await http.post<Contract>(`/employees/${employeeId}/contracts/${contract.id}/terminate`, { terminationDate }, { headers: { 'If-Match': `"${contract.version}"` } });
  return data;
}

export async function getCompensation(employeeId: string): Promise<Compensation[]> {
  const { data } = await http.get<Compensation[]>(`/employees/${employeeId}/compensation`);
  return data;
}

export async function addCompensation(employeeId: string, input: { amount: number; currency: string; frequency: string; effectiveFrom: string; effectiveTo?: string }): Promise<Compensation> {
  const { data } = await http.post<Compensation>(`/employees/${employeeId}/compensation`, input);
  return data;
}
