import { useQuery } from '@tanstack/react-query';

import { getAuditEvents, getUsers } from '../api/administration-api';

export const administrationKeys = {
  users: (cursor?: string) => ['users', { cursor }] as const,
  audit: (filters: Parameters<typeof getAuditEvents>[0]) => ['audit-events', filters] as const
};

export const useUsers = (cursor?: string, limit = 50) =>
  useQuery({ queryKey: administrationKeys.users(cursor), queryFn: () => getUsers(cursor, limit) });
export const useAuditEvents = (filters: Parameters<typeof getAuditEvents>[0]) =>
  useQuery({ queryKey: administrationKeys.audit(filters), queryFn: () => getAuditEvents(filters) });
