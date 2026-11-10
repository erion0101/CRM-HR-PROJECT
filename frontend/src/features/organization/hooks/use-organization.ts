import { useQuery } from '@tanstack/react-query';

import { getDepartments, getPositions } from '../api/organization-api';

export const organizationKeys = {
  departments: (cursor?: string, limit = 100) => ['departments', { cursor, limit }] as const,
  positions: (cursor?: string, limit = 100) => ['positions', { cursor, limit }] as const
};

export function useDepartments(cursor?: string, limit = 100) {
  return useQuery({
    queryKey: organizationKeys.departments(cursor, limit),
    queryFn: () => getDepartments(cursor, limit)
  });
}

export function usePositions(cursor?: string, limit = 100) {
  return useQuery({ queryKey: organizationKeys.positions(cursor, limit), queryFn: () => getPositions(cursor, limit) });
}
