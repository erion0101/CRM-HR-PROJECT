/* eslint-disable react-refresh/only-export-components -- the authentication provider and hook use one private context */
import { useMutation, useQuery } from '@tanstack/react-query';
import { createContext, useContext, useEffect, useMemo, type PropsWithChildren } from 'react';

import { queryClient } from '@/app/providers/query-provider';
import type { CurrentUser, Role } from '@/types/api';
import { unauthorizedEvent } from '@/lib/api-client';

import { getCurrentUser, login, logout, type LoginInput } from '../api/auth-api';

interface AuthContextValue {
  user: CurrentUser | null;
  isCheckingSession: boolean;
  isAuthenticated: boolean;
  login: (input: LoginInput) => Promise<void>;
  logout: () => Promise<void>;
  isSigningIn: boolean;
  hasRole: (...roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);
const authKey = ['auth', 'current-user'] as const;

export function AuthProvider({ children }: PropsWithChildren) {
  const session = useQuery({
    queryKey: authKey,
    queryFn: getCurrentUser,
    retry: false,
    staleTime: 60_000,
  });
  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: authKey }); },
  });
  const logoutMutation = useMutation({
    mutationFn: logout,
    onSettled: () => { queryClient.clear(); queryClient.setQueryData(authKey, null); },
  });

  useEffect(() => {
    const clearSession = () => { queryClient.setQueryData(authKey, null); };
    window.addEventListener(unauthorizedEvent, clearSession);
    return () => window.removeEventListener(unauthorizedEvent, clearSession);
  }, []);

  const user = session.data ?? null;
  const value = useMemo<AuthContextValue>(() => ({
    user,
    isCheckingSession: session.isLoading,
    isAuthenticated: Boolean(user),
    login: async (input) => { await loginMutation.mutateAsync(input); },
    logout: async () => { await logoutMutation.mutateAsync(); },
    isSigningIn: loginMutation.isPending,
    hasRole: (...roles) => Boolean(user?.roles.some((role) => roles.includes(role))),
  }), [loginMutation, logoutMutation, session.isLoading, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider.');
  return context;
}
