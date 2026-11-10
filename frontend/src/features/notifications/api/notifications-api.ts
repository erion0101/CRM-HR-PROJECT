import { http } from '@/lib/api-client';

import type { NotificationList } from '../types';

export async function getMyNotifications(): Promise<NotificationList> {
  const { data } = await http.get<NotificationList>('/notifications');
  return data;
}

export async function markNotificationRead(id: string): Promise<void> {
  await http.post(`/notifications/${id}/read`);
}

export async function markAllNotificationsRead(): Promise<void> {
  await http.post('/notifications/read-all');
}
