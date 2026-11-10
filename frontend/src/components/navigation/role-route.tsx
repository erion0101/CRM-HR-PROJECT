import type { PropsWithChildren } from 'react';
import { Navigate } from 'react-router';

import { useAuth } from '@/features/auth/hooks/use-auth';
import type { Role } from '@/types/api';

export function RoleRoute({ roles, children }: PropsWithChildren<{ roles: Role[] }>) {
  const { hasRole } = useAuth();
  return hasRole(...roles) ? children : <Navigate to="/forbidden" replace />;
}
