import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router';

import { NotificationProvider } from '@/app/providers/notification-provider';
import { QueryProvider } from '@/app/providers/query-provider';
import { router } from '@/app/router';
import { AuthProvider } from '@/features/auth/hooks/use-auth';

import './styles/index.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryProvider>
      <NotificationProvider>
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </NotificationProvider>
    </QueryProvider>
  </StrictMode>
);
