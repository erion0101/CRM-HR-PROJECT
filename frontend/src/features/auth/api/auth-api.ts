import { clearAntiforgeryToken, http } from '@/lib/api-client';
import type { CurrentUser } from '@/types/api';

export interface LoginInput {
  email: string;
  password: string;
  rememberMe: boolean;
}

export async function login(input: LoginInput): Promise<void> {
  await http.post('/auth/login', input);
  clearAntiforgeryToken();
}

export async function logout(): Promise<void> {
  await http.post('/auth/logout');
  clearAntiforgeryToken();
}

export async function getCurrentUser(): Promise<CurrentUser> {
  return (await http.get<CurrentUser>('/auth/me')).data;
}

export async function changePassword(input: { currentPassword: string; newPassword: string }): Promise<void> {
  await http.put('/auth/password', input);
}
