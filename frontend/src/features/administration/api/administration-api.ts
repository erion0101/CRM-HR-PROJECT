import { http } from '@/lib/api-client';
import type { PageResult, Role } from '@/types/api';

import type { AuditEvent, ManagedUser } from '../types';

export async function getUsers(cursor?: string, limit = 50): Promise<PageResult<ManagedUser>> {
  const { data } = await http.get<PageResult<ManagedUser>>('/users', { params: { cursor, limit } });
  return data;
}

export async function createUser(input: { email: string; temporaryPassword: string; role: Role; employeeId?: string }): Promise<ManagedUser> {
  const { data } = await http.post<ManagedUser>('/users', input);
  return data;
}

export async function updateUser(user: ManagedUser, input: { isActive: boolean; role: Role; employeeId?: string }): Promise<ManagedUser> {
  const { data } = await http.put<ManagedUser>(`/users/${user.id}`, input);
  return data;
}

export async function getAuditEvents(filters: { entityType?: string; userId?: string; from?: string; cursor?: string; limit?: number } = {}): Promise<PageResult<AuditEvent>> {
  const { data } = await http.get<PageResult<AuditEvent>>('/audit-events', { params: filters });
  return data;
}
