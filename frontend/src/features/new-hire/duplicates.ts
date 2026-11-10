import { getEmployees } from '@/features/employees/api/employees-api';
import type { Employee } from '@/features/employees/types';

export interface DuplicateEmployee {
  field: 'workEmail' | 'employeeNumber';
  employee: Pick<Employee, 'id' | 'firstName' | 'lastName' | 'employeeNumber' | 'workEmail'>;
}

/** Exact, case-insensitive matches only; the API search is a "contains" search. */
export function exactDuplicates(
  employees: DuplicateEmployee['employee'][],
  workEmail: string,
  employeeNumber: string
): DuplicateEmployee[] {
  const email = workEmail.trim().toLowerCase();
  const number = employeeNumber.trim().toLowerCase();
  const found: DuplicateEmployee[] = [];
  for (const employee of employees) {
    if (email && employee.workEmail.toLowerCase() === email) found.push({ field: 'workEmail', employee });
    if (number && employee.employeeNumber.toLowerCase() === number) found.push({ field: 'employeeNumber', employee });
  }
  return found;
}

/** Looks up existing employees with the same work email or employee number before anything is created. */
export async function findDuplicateEmployees(workEmail: string, employeeNumber: string): Promise<DuplicateEmployee[]> {
  const [byEmail, byNumber] = await Promise.all([
    getEmployees({ search: workEmail.trim(), limit: 10 }),
    getEmployees({ search: employeeNumber.trim(), limit: 10 })
  ]);
  const unique = new Map([...byEmail.items, ...byNumber.items].map((employee) => [employee.id, employee]));
  return exactDuplicates([...unique.values()], workEmail, employeeNumber);
}
