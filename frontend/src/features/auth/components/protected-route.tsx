import { Navigate, Outlet, useLocation } from 'react-router';

import { Skeleton } from '@/components/feedback/skeleton';

import { useAuth } from '../hooks/use-auth';

export function ProtectedRoute() {
  const location = useLocation();
  const { isAuthenticated, isCheckingSession } = useAuth();
  if (isCheckingSession)
    return (
      <div className="full-page-loader" aria-label="Checking your session">
        <Skeleton className="skeleton--panel" />
      </div>
    );
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <Outlet />;
}
