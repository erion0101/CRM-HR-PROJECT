export interface PageResult<T> {
  items: T[];
  nextCursor: string | null;
}

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}

export type Role = 'Employee' | 'Manager' | 'HR' | 'ProductManager';

export interface CurrentUser {
  userId: string;
  organizationId: string;
  employeeId: string | null;
  email: string;
  roles: Role[];
}
