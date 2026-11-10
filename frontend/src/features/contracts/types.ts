export type ContractStatus = 'Draft' | 'Active' | 'Expired' | 'Terminated';
export interface Contract {
  id: string;
  employeeId: string;
  contractNumber: string;
  status: ContractStatus;
  startDate: string;
  endDate: string | null;
  revision: number;
  version: number;
}
export interface Compensation {
  id: string;
  employeeId: string;
  amount: number;
  currency: string;
  frequency: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  version: number;
}
